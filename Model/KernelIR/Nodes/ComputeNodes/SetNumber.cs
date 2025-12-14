using DGEMMSharp.Model.KernelIR.Values;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DGEMMSharp.Model.KernelIR.Nodes.ComputeNodes
{
    public class SetNumber(KernelDef kernel, IVariable<int> dest, 
        IScalar<int> src, IScalar<int>? offset = null, 
        IScalar<int>? step = null) 
        : ComputeBase<IVariable<int>>(kernel, dest)
    {
        public IScalar<int> Src { get; } = src;

        public IScalar<int>? Offset { get; } = offset;

        public IScalar<int>? Step { get; } = step;

        public override IEnumerable<ISrc> GetUsings()
        {
            yield return Src;
            if (Offset != null)
                yield return Offset;
            if (Step != null) 
                yield return Step;
        }

        public override void Emit()
        {
            Src.LoadValue();
            if (Offset is not null)
            {
                Offset.LoadValue();
                if (Step is not null)
                {
                    Step.LoadValue();
                    Emitter.Multiply();
                }
                Emitter.Add();
            }
            Dest.StoreValue();
        }

        public override string ToString()
        {
            return Offset is null ?
                $"{Dest} = {Src}" :
                (Step is null ?
                    $"{Dest} = {Src} + {Offset}" :
                    $"{Dest} = {Src} + {Offset} * {Step}");
        }
    }
}
