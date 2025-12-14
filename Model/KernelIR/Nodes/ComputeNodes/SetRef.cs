using DGEMMSharp.Model.KernelIR.Values;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DGEMMSharp.Model.KernelIR.Nodes.ComputeNodes
{
    public class SetRef(KernelDef kernel, MemRef dest,
        MemRef src, IScalar<int>? offset = null, 
        IScalar<int>? step = null)
        : ComputeBase<MemRef>(kernel, dest)
    {
        public MemRef Src { get; } = src;

        public IScalar<int>? Offset { get; } = offset;

        public IScalar<int>? Step { get; } = step;

        public override IEnumerable<ISrc> GetUsings()
        {
            yield return Src;
            if (Offset is not null)
            {
                yield return Offset;
            }
            if (Step is not null)
            {
                yield return Step;
            }
        }

        public override void Emit()
        {
            Src.LoadAddr();
            if (Offset is not null)
            {
                Offset.LoadValue();
                if (Step is not null)
                {
                    Step.LoadValue();
                    Emitter.Multiply();
                }
                Emitter.Call(ILUtils.AddRefDouble, null);
            }
            Dest.StoreAddr();
        }

        public override string ToString()
        {
            return Offset is null ?
                $"{Dest} = ref {Src}" :
                (Step is null ?
                    $"{Dest} = ref {Src}[{Offset}]" :
                    $"{Dest} = ref {Src}[{Offset} * {Step}]");
        }
    }
}
