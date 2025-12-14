using DGEMMSharp.Model.KernelIR.Values;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DGEMMSharp.Model.KernelIR.Nodes.ComputeNodes
{
    public class StoreVec(KernelDef kernel, MemRef dest,
        Register src) : ComputeBase<MemRef>(kernel, dest)
    {
        public Register Src { get; } = src;

        public override IEnumerable<ISrc> GetUsings()
        {
            yield return Src;
        }

        public override void Emit()
        {
            Src.LoadValue();
            Dest.LoadAddr();
            Emitter.Call(Config.StoreVector, null);
        }

        public override string ToString()
        {
            return $"{Dest} = Store({Src})";
        }
    }
}
