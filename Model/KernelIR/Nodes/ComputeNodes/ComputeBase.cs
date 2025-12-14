using DGEMMSharp.Model.KernelIR.Values;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DGEMMSharp.Model.KernelIR.Nodes.ComputeNodes
{
    public abstract class ComputeBase(KernelDef kernel, IDest dest)
        : Node(kernel)
    {
        public IDest DestValue { get; } = dest;

        public abstract IEnumerable<ISrc> GetUsings();
    }

    public abstract class ComputeBase<TDest>(KernelDef kernel,
        TDest dest) : ComputeBase(kernel, dest) where TDest : IDest
    {
        public TDest Dest => (TDest)DestValue;
    }
}
