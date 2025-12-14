using DGEMMSharp.Model.KernelIR.Values;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DGEMMSharp.Model.KernelIR.Nodes.ComputeNodes
{
    public class LoadVec(KernelDef kernel, Register dest,
        MemRef src, IScalar<int>? offset = null)
        : ComputeBase<Register>(kernel, dest)
    {
        public MemRef Src { get; } = src;

        public IScalar<int>? Offset { get; } = offset;

        public override IEnumerable<ISrc> GetUsings()
        {
            yield return Src;
            if (Offset is not null)
            {
                yield return Offset;
            }
        }

        public override void Emit()
        {
            Src.LoadAddr();
            if (Offset is null)
            {
                Emitter.Call(Config.LoadVector, null);
            }
            else
            {
                Offset.LoadValue();
                Emitter.Convert<nint>();
                Emitter.Call(Config.LoadVectorWithOffset, null);
            }
            Dest.StoreValue();
        }

        public override string ToString()
        {
            return
                Offset is null ?
                $"{Dest} = Load({Src})" :
                $"{Dest} = Load({Src}[{Offset}])";
        }
    }
}
