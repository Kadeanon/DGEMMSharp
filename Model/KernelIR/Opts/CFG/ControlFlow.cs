using DGEMMSharp.Model.KernelIR.Nodes;
using NetFabric;
using Sigil;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DGEMMSharp.Model.KernelIR.Opts.CFG
{
    public class ControlFlow
    {
        public KernelDef Def { get; }

        public BasicBlock Root { get; }

        public Dictionary<Label, BasicBlock> Graph { get; }

        public ControlFlow(KernelDef kernel)
        {
            Def = kernel;
            Graph = [];
            DoublyLinkedList<Node> nodes = kernel.Nodes;
            int index = 0;
            Root = BasicBlock.Build(nodes.Forward,
                ref index);
            while (index < nodes.Count)
            {
                var bb = BasicBlock.Build(nodes.Forward, 
                    ref index);
                var mark = bb.HeadMark;
                Graph!.Add(mark!.Label, bb);
            }
        }

        public void Dump()
        {
#if DEBUG
            Console.WriteLine(Root);

            foreach (var node in Graph.Values)
            {
                Console.WriteLine(node.ToString());
            }
            Console.WriteLine();
#endif
        }
    }
}
