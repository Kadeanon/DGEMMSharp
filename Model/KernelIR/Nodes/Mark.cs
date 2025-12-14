using Sigil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DGEMMSharp.Model.KernelIR.Nodes
{
    public class Mark(KernelDef kernel,
        string name) : Node(kernel)
    {
        public string Name => Label.Name;

        public Label Label { get; }
            = kernel.GetOrDefineLabel(name);

        public override void Emit()
        {
            Emitter.MarkLabel(Label);
        }

        public override string ToString()
        {
            return Name + ':';
        }
    }
}
