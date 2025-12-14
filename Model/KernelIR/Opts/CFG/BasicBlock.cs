using DGEMMSharp.Model.KernelIR.Nodes;
using DGEMMSharp.Model.KernelIR.Nodes.ComputeNodes;
using DGEMMSharp.Model.KernelIR.Nodes.ControlNodes;
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
    public class BasicBlock
    {
        public string Name { get; private set; }

        public Mark? HeadMark { get; private set; }

        public ControlBase? TailNode { get; private set; }

        public DoublyLinkedList<ComputeBase> Nodes { get; private set; }

        private BasicBlock(string name) 
        {
            Name = name;
            HeadMark = null;
            Nodes = new DoublyLinkedList<ComputeBase>();
        }

        private BasicBlock(Mark head)
        {
            Name = head.Name;
            HeadMark = head;
            Nodes = new DoublyLinkedList<ComputeBase>();
        }

        public static BasicBlock Build(
            IReadOnlyList<Node> nodes, ref int index)
        {
            BasicBlock bb;
            if(index == 0)
            {
                bb = new("<>Start");
            }
            else if (nodes[index] is Mark mark)
            {
                bb = new(mark);
            }
            else
            {
                throw new InvalidOperationException();
            }

            while (++index < nodes.Count)
            {
                var node = nodes[index];
                if (node is ControlBase control)
                {
                    bb.TailNode = control;
                    index++;
                    Debug.Assert(index == nodes.Count ||
                        nodes[index] is Mark);
                    break;
                }
                else if(node is ComputeBase compute)
                {
                    bb.Nodes.AddLast(compute);
                }
                else
                {
                    throw new InvalidOperationException();
                }
            }

            return bb;
        }

        public override string ToString()
        {
            StringBuilder sb = 
                new StringBuilder()
                .Append(Name)
                .AppendLine(":")
                .AppendJoin(Environment.NewLine, 
                Nodes.Forward);
            sb.AppendLine();
            if(TailNode != null)
                sb.AppendLine(TailNode.ToString());
            return sb.ToString();
        }
    }
}
