using Sigil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DGEMMSharp.Model.KernelIR.Nodes.ControlNodes
{
    public abstract class ControlBase(KernelDef kernel,
        string name) : Node(kernel)
    {
        public string Name => Label.Name;

        public Label Label { get; }
            = kernel.GetOrDefineLabel(name);

        public virtual IEnumerable<Label> NextLabels()
        {
            yield return Label;
        }
    }
}
