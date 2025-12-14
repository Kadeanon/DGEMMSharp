using Sigil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DGEMMSharp.Model.KernelIR.Nodes.ControlNodes
{
    public class Jump(KernelDef kernel,
        string name) : ControlBase(kernel, name)
    {
        public override void Emit()
        {
            Emitter.Branch(Label);
        }

        public override string ToString()
        {
            return $"goto {Name}";
        }
    }
}
