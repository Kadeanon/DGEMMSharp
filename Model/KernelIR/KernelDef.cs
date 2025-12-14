using DGEMMSharp.Model.KernelIR;
using DGEMMSharp.Model.KernelIR.Nodes;
using DGEMMSharp.Model.KernelIR.Nodes.ComputeNodes;
using DGEMMSharp.Model.KernelIR.Nodes.ControlNodes;
using DGEMMSharp.Model.KernelIR.Opts;
using DGEMMSharp.Model.KernelIR.Opts.CFG;
using DGEMMSharp.Model.KernelIR.Values;
using NetFabric;
using Sigil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.X86;
using System.Text;
using System.Threading.Tasks;
using Kernel = DGEMMSharp.Model.DGEMM.Kernel;

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

        public DoublyLinkedList<Node> Nodes { get; private set; }

        private Dictionary<string, Label> LabelDict { get; set; }

        public ControlFlow? CFG { get; set; }

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
            ParamK = new ParamNumber(this, 0, "k");
            ParamARef = new ParamMemRef(this, 1, "a");
            ParamBRef = new ParamMemRef(this, 2, "b");
            ParamCRef = new ParamMemRef(this, 3, "c");
            ParamLDC = new ParamNumber(this, 4, "ldc");
            LocalP = new LocalNumber(this, "p");
            AVecs = new Register[MR];
            BVecs = new Register[NV];
            CVecs = new Register[MR * NV];
            CRefs = new LocalMemRef[MR * NV];
            for (int i = 0; i < MR; i++)
            {
                AVecs[i] = new Register(this, $"aVec{i}");
                for (int j = 0; j < NV; j++)
                {
                    CVecs[i * NV + j] = new Register(this, $"cVec{i}{j}");
                    CRefs[i * NV + j] = new LocalMemRef(this, $"cRef{i}{j}");
                }
            }
            for (int i = 0;i < NV; i++)
            {
                BVecs[i] = new Register(this, $"bVec{i}");
            }
            Nodes = new();
            LabelDict = [];
        }

        public Local DefRegister(string name)
        {
            return Emitter.DeclareLocal(Config.SIMDType, name);
        }

        public Local DefRef<T>(string name)
        {
            return Emitter.DeclareLocal(
                typeof(T).MakeByRefType(), name);
        }

        public Local DefVariable<T>(string name)
        {
            return Emitter.DeclareLocal<T>(name);
        }

        internal Kernel BuildKernel()
        {
            BuildNodes();
            CFG.Dump();
            OptNodes();
            CFG.Dump();
            return Emit();
        }

        #region BuildNodes
        internal void BuildNodes()
        {
            BuildLoadVectorC();
            BuildCycleFMA();
            BuildStoreVectorC();

            DoublyLinkedList<Node>.Node? head = Nodes.First;
            while(head is not null)
            {
                var prev = head.Value;
                var next = head.Next;
                var succ = next?.Value;
                if(prev is ControlBase control &&
                    succ is not Mark && succ is not null)
                {
                    var nextHead = new Mark(this, 
                        control.Label.Name + "_next");
                    if (prev is Condition cond)
                    {
                        head.Value = new Branch(cond, nextHead);
                    }
                    Nodes.AddAfter(head, nextHead);
                }
                else if(prev is not ControlBase && 
                    succ is Mark mark)
                {
                    Nodes.AddAfter(head,
                        new Jump(this, mark.Label.Name));
                }

                head = next;
            }

            CFG = new(this);
        }

        private void BuildCycleFMA()
        {
            //loop 
            //int p = 0;
            var zeroConst = Constant(0);
            var oneConst = Constant(1);
            Nodes.AddLast(new SetNumber(this, 
                LocalP, zeroConst));

            // Continue:
            Nodes.AddLast(new Mark(this, "Continue"));

            // if ( p >= k) goto Break;
            Nodes.AddLast(new Condition(this, "Break",
                LocalP, ParamK, BinaryOpKind.Ge));

            for (int j = 0; j < NV; j++)
            {
                Nodes.AddLast(new LoadVec(this, BVecs[j], 
                    ParamBRef, Constant(j * VecSize)));
            }

            var vecConst = Constant(VecSize * NV);
            Nodes.AddLast(new SetRef(this, 
                ParamBRef, ParamBRef, vecConst));

            // FMA
            for (int i = 0; i < MR; i++)
            {
                var aVec = AVecs[i];
                Nodes.AddLast(new Broadcast(this, aVec, ParamARef));
                for (int j = 0; j < NV; j++)
                {
                    // cVec += aVec * bVec;
                    Nodes.AddLast(new MulAdd(this, 
                        CVecs[i * NV + j], aVec, BVecs[j]));
                }
                Nodes.AddLast(new SetRef(this, 
                    ParamARef, ParamARef, oneConst));
            }

            // p++
            Nodes.AddLast(new SetNumber(this, LocalP, LocalP, Constant(1)));

            // goto Continue;
            Nodes.AddLast(new Jump(this, "Continue"));
            // Break:
            Nodes.AddLast(new Mark(this, "Break"));
        }

        private void BuildLoadVectorC()
        {
            MemRef prev = ParamCRef;
            var vecSizeConst = Constant(VecSize);
            for (int i = 0; i < MR; i++)
            {
                var loopConst = Constant(i);
                for (int j = 0; j < NV; j++)
                {
                    var idx = i * NV + j;
                    var refDef = CRefs[idx];
                    var vecDef = CVecs[idx];
                    if (j == 0)
                    {
                        Nodes.AddLast(new SetRef(this, refDef, 
                            ParamCRef, loopConst, ParamLDC));
                    }
                    else
                    {
                        Nodes.AddLast(new SetRef(this,
                            refDef, prev, vecSizeConst));
                    }
                    Nodes.AddLast(new LoadVec(this, vecDef, refDef));
                    prev = refDef;
                }
            }
        }

        private void BuildStoreVectorC()
        {
            for (int i = 0; i < CRefs.Length; i++)
            {
                var v = CRefs[i];
                var r = CVecs[i];
                Nodes.AddLast(new StoreVec(this, v, r));
            }
        }

        private Constant<T> Constant<T>(T value) 
            => new(this, value);

        internal Label GetOrDefineLabel(string labelName)
        {
            ref var slot = ref CollectionsMarshal
                .GetValueRefOrAddDefault(LabelDict, labelName, out var exists);
            if(!exists)
            {
                Emitter.DefineLabel(out slot, labelName);
            }
            return slot!;
        }
        #endregion BuildNodes

        #region Opt
        private void OptNodes()
        {
            var opt = new MicroOpt(this);
            opt.Invoke();
        }
        #endregion Opt

        #region Emit
        private Kernel Emit()
        {
            foreach (var expr in Nodes.Forward)
            {
                expr.Emit();
            }

            return Emitter.Return().CreateDelegate();
        }
        #endregion Emit
    }
}
