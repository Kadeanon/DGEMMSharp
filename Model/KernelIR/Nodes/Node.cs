using Sigil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DGEMMSharp.Model.KernelIR.Nodes
{
    public abstract class Node(KernelDef def)
    {
        public KernelDef Kernel { get; } = def;

        public Emit<DGEMM.Kernel> Emitter => Kernel.Emitter;

        public RuntimeConfig Config => Kernel.Config;

        public abstract void Emit();
    }
}
