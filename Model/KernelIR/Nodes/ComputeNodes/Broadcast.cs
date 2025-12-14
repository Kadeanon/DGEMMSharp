using DGEMMSharp.Model.KernelIR.Values;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DGEMMSharp.Model.KernelIR.Nodes.ComputeNodes
{
    public class Broadcast(KernelDef kernel, Register dest,
        MemRef src) : ComputeBase<Register>(kernel, dest)
    {
        public MemRef Src { get; } = src;

        public override IEnumerable<ISrc> GetUsings()
        {
            yield return Src;
        }

        public override void Emit()
        {
            Src.LoadValue();
            Emitter.Call(Config.BroadcastVector, null);
            Dest.StoreValue();
        }

        public override string ToString()
        {
            return $"{Dest} = Broadcast({Src})";
        }
    }
}
