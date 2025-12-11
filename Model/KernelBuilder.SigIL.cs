using Sigil;
using System.Reflection;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

//using static DGEMMSharp.Model.DGEMM;

using Kernel = DGEMMSharp.Model.DGEMM.Kernel;

namespace DGEMMSharp.Model;

public class SigILEmitter
{
    internal VectorType VectorLevel { get; }
        
    public Type StaticSIMDType { get; }

    public Type SIMDType { get; }

    public Type doubleRefType { get; }

    internal Emit<Kernel>? Emitter { get; private set; }

    public Local[] CRefs { get; private set; }

    public Local[] CSpans { get; private set; }

    public Local[] CVecs { get; private set; }
    public Local[] BVars { get; private set; } 

    public int MR { get; private set; }

    public int NR { get; private set; }

    public int VecSize => VectorLevel switch
    {
        VectorType.Vector128 => 2,
        VectorType.Vector256 => 4,
        VectorType.Vector512 => 8,
        _ => throw new NotSupportedException("Unsupported yet")
    };

    public int NV => NR / VecSize;

    public SigILEmitter()
    {
        VectorLevel = DGEMM.VectorLevel;
        StaticSIMDType = DGEMM.StaticSIMDType;
        SIMDType = DGEMM.SIMDType;
        doubleRefType = typeof(double).MakeByRefType();
    }

    /// <summary>
    /// Dynamically generates an optimized microkernel using IL code emission.
    /// </summary>
    /// <remarks>
    /// 1. Creates vector registers for C matrix blocks<br />
    /// 2. Generates FMA instructions for vector operations<br />
    /// 3. Automatically adapts to available SIMD capabilities<br />
    /// Uses System.Reflection.Emit to generate hardware-specific optimizations at runtime.
    /// </remarks>
    internal Kernel BuildKernel(int mr, int nr)
    {
        MR = mr;
        NR = nr;
        CRefs = new Local[NV];
        CVecs = new Local[MR * NV];
        CSpans = new Local[MR * NV];
        BVars = new Local[NV];
        Emitter = Emit<Kernel>.NewDynamicMethod();
        BuildLoadVectorC();
        BuildCycleFMA();
        BuildStoreVectorC();
        Emitter.Return();
        return Emitter.CreateDelegate();
    }

    private void BuildLoadVectorC()
    {
        // define and load cRefs
        for (int j = 0; j < NV; j++)
        {
            var local =
                Emitter.DeclareLocal(doubleRefType);
            if (j == 0)
            {
                Emitter.LoadArgument(3);
                Emitter.StoreLocal(local);
        }
            else
        {
            EmitAddRef(CRefs[j - 1],
                    VecSize, local);
            }
            CRefs[j] = local;
        }

        // define the local variables for cSpan and cVec
        // and then load the cSpan
        for (int i = 0; i < MR; i++)
        {
            for (int j = 0; j < NV; j++)
            {
                DeclAndInitSpanC(i, j);
                DeclAndLoadVectorC(i, j);

                if (i < MR - 1) UpdateRefC(j);
            }
        }
    }

    private void DeclAndInitSpanC(int i, int j)
    {
        // Span<double> cSpanij = MemoryMarshal.CreateSpan(ref ci, vectorSize);
        var spanDef = Emitter.DeclareLocal(typeof(Span<double>));
        MethodInfo createSpan = ILUtils.CreateSpan;
        Emitter.LoadLocal(CRefs[j]);
        Emitter.LoadConstant(VecSize);
        Emitter.Call(createSpan, null);
        Emitter.StoreLocal(spanDef);
        CSpans[i * NV + j] = spanDef;
    }

    private void DeclAndLoadVectorC(int i, int j)
    {
        var spanDef = CSpans[i * NV + j];
        // VectorX<double> cVecij = Vector<double>.Create(cSpanij);
        var vecDef = Emitter.DeclareLocal(SIMDType);
        MethodInfo createVector = 
            ILUtils.DynamicCreateVectorFromSpan(StaticSIMDType);
        MethodInfo implictConv = ILUtils.ConvSpanAsReadOnly;
        Emitter.LoadLocal(spanDef);
        Emitter.Call(implictConv, null);
        Emitter.Call(createVector, null);
        Emitter.StoreLocal(vecDef);
        CVecs[i * NV + j] = vecDef;
    }

    private void UpdateRefC(int j)
    {
        // ci = ref Unsafe.Add(ref ci, ldc);
        Emitter.LoadLocal(CRefs[j]);
        Emitter.LoadArgument(4);
        Emitter.Call(ILUtils.AddRefDouble, null);
        Emitter.StoreLocal(CRefs[j]);
    }

    private void BuildCycleFMA()
    {
        Type doubleRefType = typeof(double).MakeByRefType();
        int simdSize = VecSize;
        Type simdType = SIMDType;
        MethodInfo createVectorFromScalar = ILUtils.DynamicCreateVectorFromScalar(StaticSIMDType);

        // ref double aRef = ref a;
        var aRef = Emitter.DeclareLocal(doubleRefType);
        Emitter.LoadArgument(1);
        Emitter.StoreLocal(aRef);
        // ref double aRef = ref a;
        var bRef = Emitter.DeclareLocal(doubleRefType);
        Emitter.LoadArgument(2);
        Emitter.StoreLocal(bRef);

        //loop 
        //int p = 0
        var p = Emitter.DeclareLocal<int>();
        Emitter.LoadConstant(0);
        Emitter.StoreLocal(p);
        Label loopCmp = Emitter.DefineLabel();
        Label loopBegin = Emitter.DefineLabel();
        Emitter.Branch(loopCmp);
        Emitter.MarkLabel(loopBegin);

        Local[] bVars = new Local[MR];
        // define the local variables for b
        //Vector256<double> bVec0 = Vector256.LoadUnsafe(ref bRef);
        //{
        //var loadUnsafe = ILUtils.DynamicLoadVectorUnsafe(StaticSIMDType);
        //var bVec = emitter.DeclareLocal(simdType);
        //bVars[0] = bVec;
        //emitter.LoadLocal(bRef);
        //emitter.Call(loadUnsafe, null);
        //emitter.StoreLocal(bVec);
        //}
        //Vector256<double> bVec1 = Vector256.LoadUnsafe(ref bRef, 4);
        var loadUnsafeWithOffset =
        ILUtils.DynamicLoadVectorUnsafeWithOffset(StaticSIMDType);
        for (int j = 0; j < NV; j++)
        {
            var bVec = Emitter.DeclareLocal(simdType);
            bVars[j] = bVec;
            BuildLocalVectorB(simdSize,
                bRef, loadUnsafeWithOffset, j);
        }
        EmitAddRef(bRef, simdSize * NV);

        // FMA
        for (int i = 0; i < MR; i++)
        {
            // define the local variables for a
            var aVec = Emitter.DeclareLocal(simdType);
            Emitter.LoadLocal(aRef);
            Emitter.LoadIndirect<double>();
            Emitter.Call(createVectorFromScalar, null);
            Emitter.StoreLocal(aVec);
            for (int j = 0; j < NV; j++)
            {
                var cVec = CVecs[i * NV + j];
                var bVec = BVars[j];
                EmitFMA(aVec, bVec, cVec);
            }
            EmitAddRef(aRef, 1);
        }

        // p++
        Emitter.LoadLocal(p);
        Emitter.LoadConstant(1);
        Emitter.Add();
        Emitter.StoreLocal(p);

        Emitter.MarkLabel(loopCmp);

        // p < k
        Emitter.LoadLocal(p);
        Emitter.LoadArgument(0);
        Emitter.BranchIfLess(loopBegin);
    }

    private void BuildLocalVectorB(int simdSize, Local bRef,
        MethodInfo loadUnsafeWithOffset, int j)
    {
        var bVec = Emitter.DeclareLocal(SIMDType);
        Emitter.LoadLocal(bRef);
        Emitter.LoadConstant(j * simdSize);
        Emitter.Convert<nint>();
        Emitter.Call(loadUnsafeWithOffset, null);
        Emitter.StoreLocal(bVec);
        BVars[j] = bVec;
    }

    private void BuildStoreVectorC()
    {
        MethodInfo copyTo = ILUtils.DynamicCopyTo(StaticSIMDType);

        for (int i = 0; i < CSpans.Length; i++)
        {
            var cSpan = CSpans[i];
            var cVec = CVecs[i];
            Emitter.LoadLocal(cVec);
            Emitter.LoadLocal(cSpan);
            Emitter.Call(copyTo, null);
        }
    }
    /// <summary>
    /// local = ref Unsafe.Add(ref local, offset);
    /// </summary>
    internal void EmitAddRef(Local local, int offset)
    => EmitAddRef(local, offset, local);

    /// <summary>
    /// dest = ref Unsafe.Add(ref src, offset);
    /// </summary>
    internal void EmitAddRef(Local src, int offset, Local dest)
    {
        Emitter.LoadLocal(src);
        Emitter.LoadConstant(offset);
        Emitter.Call(ILUtils.AddRefDouble, null);
        Emitter.StoreLocal(dest);
    }

    /// <summary>
    /// c = Fma.MultiplyAdd(a, b, c); <=> c += a * b;
    /// </summary>
    /// <param name="Emitter"></param>
    /// <param name="simdType"></param>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <param name="c"></param>
    internal void EmitFMA(Local a, Local b, Local c)
    {
        Emitter.LoadLocal(a);
        Emitter.LoadLocal(b);
        Emitter.LoadLocal(c);
        var fma = typeof(Fma).
            GetMethod("MultiplyAdd", 
            BindingFlags.Public | BindingFlags.Static,
            [SIMDType, SIMDType, SIMDType]);
        Emitter.Call(fma, null);
        Emitter.StoreLocal(c);
    }
}
