using DGEMMSharp.Model.KernelIR.Values;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DGEMMSharp.Model.KernelIR.Nodes.ComputeNodes
{
    internal class MulAdd(KernelDef kernel, Register dest,
        Register x, Register y)
        : ComputeBase<Register>(kernel, dest)
    {
        public Register X { get; } = x;

        public Register Y { get; } = y;

        public override IEnumerable<ISrc> GetUsings()
        {
            yield return X;
            yield return Y;
            yield return Dest;
        }

        public override void Emit()
        {
            X.LoadValue();
            Y.LoadValue();
            Dest.LoadValue();
            Emitter.Call(Config.MultiAdd, null);
            Dest.StoreValue();
        }

        public override string ToString()
        {
            return $"{Dest} = {X} * {Y} + {Dest}";
        }
    }
}
