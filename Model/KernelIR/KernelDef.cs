using DGEMMSharp.Model.KernelIR;
using DGEMMSharp.Model.KernelIR.Values;
using Sigil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Intrinsics.X86;
using System.Text;
using System.Threading.Tasks;
using Kernel= DGEMMSharp.Model.DGEMM.Kernel;

namespace DGEMMSharp.Model.KernelIR
{
    public class KernelDef
    {
        public RuntimeConfig Config { get; }

        public string Prefix { get; }

        public string FullName => $"{Prefix}_{MR}x{NR}";

        public Emit<Kernel> Emitter { get; }

        public ParamNumber ParamK { get; }

        public ParamMemRef ParamARef { get; }

        public ParamMemRef ParamBRef { get; }

        public ParamMemRef ParamCRef { get; }

        public ParamNumber ParamLDC { get; }

        public LocalNumber LocalP { get; }

        public Register[] AVecs { get; private set; }

        public Register[] BVecs { get; private set; }

        public Register[] CVecs { get; private set; }

        public MemRef[] CRefs { get; private set; }

        public int MR { get; private set; }

        public int NR { get; private set; }

        public int VecSize => Config.VectorLevel switch
        {
            VectorType.Vector128 => 2,
            VectorType.Vector256 => 4,
            VectorType.Vector512 => 8,
            _ => throw new NotSupportedException("Unsupported yet")
        };

        public int NV => NR / VecSize;

        public KernelDef(int mr, int nr, RuntimeConfig config, string prefix = "")
        {
            MR = mr;
            NR = nr;
            Config = config;
            Prefix = prefix;
            Emitter = Emit<DGEMM.Kernel>.NewDynamicMethod(FullName);
            ParamK = new ParamNumber(this, 0);
            ParamARef = new ParamMemRef(this, 1);
            ParamBRef = new ParamMemRef(this, 2);
            ParamCRef = new ParamMemRef(this, 3);
            ParamLDC = new ParamNumber(this, 4);
            LocalP = new LocalNumber(this);
            AVecs = new Register[MR];
            BVecs = new Register[NV];
            CVecs = new Register[MR * NV];
            CRefs = new LocalMemRef[MR * NV];
            for (int i = 0; i < MR; i++)
            {
                AVecs[i] = new Register(this);
                for (int j = 0; j < NV; j++)
                {
                    CVecs[i * NV + j] = new Register(this);
                    CRefs[i * NV + j] = new LocalMemRef(this);
                }
            }
            for (int i = 0;i < NV; i++)
            {
                BVecs[i] = new Register(this);
            }
        }

        public Local DefRegister()
        {
            return Emitter.DeclareLocal(Config.SIMDType);
        }

        public Local DefRef<T>()
        {
            return Emitter.DeclareLocal(typeof(T).MakeByRefType());
        }

        public Local DefVariable<T>()
        {
            return Emitter.DeclareLocal<T>();
        }

        internal Kernel BuildKernel()
        {
            BuildLoadVectorC();
            BuildCycleFMA();
            BuildStoreVectorC();
            Emitter.Return();
            return Emitter.CreateDelegate();
        }

        private void BuildCycleFMA()
        {
            //loop 
            //int p = 0;
            LocalP.Set(0);
            Label loopContinue = Emitter.DefineLabel();
            Label loopBreak = Emitter.DefineLabel();

            // Continue:
            Emitter.MarkLabel(loopContinue);

            // if ( p >= k) goto Break;
            Emitter.LoadLocal(LocalP.Local);
            Emitter.LoadArgument(0);
            Emitter.BranchIfGreaterOrEqual(loopBreak);

            for (int j = 0; j < NV; j++)
            {
                BVecs[j].LoadWithOffset(ParamBRef, j * VecSize);
            }
            ParamBRef.UpdateRef(VecSize * NV);

            // FMA
            for (int i = 0; i < MR; i++)
            {
                var aVec = AVecs[i];
                aVec.Broadcast(ParamARef);
                ParamARef.IncRef();
                for (int j = 0; j < NV; j++)
                {
                    // cVec += aVec * bVec;
                    CVecs[i * NV + j].UpdateFMA(aVec, BVecs[j]);
                }
            }

            // p++
            LocalP.Inc();

            // goto Continue;
            Emitter.Branch(loopContinue);
            // Break:
            Emitter.MarkLabel(loopBreak);
        }

        private void BuildLoadVectorC()
        {
            MemRef prev = ParamCRef;
            for (int i = 0; i < MR; i++)
            {
                for (int j = 0; j < NV; j++)
                {
                    var idx = i * NV + j;
                    var refDef = CRefs[idx];
                    var vecDef = CVecs[idx];
                    if (j == 0)
                    {
                        refDef.SetByRefAndOffset(ParamCRef, i, ParamLDC);
                    }
                    else
                    {
                        refDef.SetByRefAndOffset(prev, VecSize);
                    }
                    vecDef.Load(refDef);
                    prev = refDef;
                }
            }
        }

        private void BuildStoreVectorC()
        {
            for (int i = 0; i < CVecs.Length; i++)
            {
                CVecs[i].Store(CRefs[i]);
            }
        }
    }
}
