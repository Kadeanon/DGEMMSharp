using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DGEMMSharp.Model.KernelIR.Nodes.ControlNodes
{
    public class Branch(Condition cond, Mark next) :
        ControlBase(cond.Kernel, cond.Name)
    {
        public Condition Base { get; } = cond;

        public Mark Next { get; } = next;

        public override void Emit() => Base.Emit();

        public override string ToString()
        {
            return $"{Base} else {Next.Name}";
        }
    }
}
