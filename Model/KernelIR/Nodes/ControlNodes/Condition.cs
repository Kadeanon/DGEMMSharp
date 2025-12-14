using DGEMMSharp.Model.KernelIR.Values;
using Sigil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DGEMMSharp.Model.KernelIR.Nodes.ControlNodes
{
    public class Condition(KernelDef kernel, string name,
        IScalar left, IScalar right, BinaryOpKind binaryOp)
        : ControlBase(kernel, name)
    {
        public IScalar Left { get; } = left;

        public IScalar Right { get; } = right;

        public BinaryOpKind BinaryOp { get; } = binaryOp;

        public override void Emit()
        {
            Left.LoadValue();
            Right.LoadValue();
            switch (BinaryOp)
            {
                case BinaryOpKind.Eq:
                    Emitter.BranchIfEqual(Label);
                    break;
                case BinaryOpKind.Ne:
                    Emitter.BranchIfFalse(Label);
                    break;
                case BinaryOpKind.Lt:
                    Emitter.BranchIfLess(Label);
                    break;
                case BinaryOpKind.Le:
                    Emitter.BranchIfLessOrEqual(Label);
                    break;
                case BinaryOpKind.Gt:
                    Emitter.BranchIfGreater(Label);
                    break;
                case BinaryOpKind.Ge:
                    Emitter.BranchIfGreaterOrEqual(Label);
                    break;
            }
        }

        public override string ToString()
        {
            return $"br {Label.Name} if {Left} {BinaryOp} {Right}";
        }
    }

    public enum BinaryOpKind
    {
        Eq, // ==
        Ne, // !=
        Lt, // <
        Le, // <=
        Gt, // >
        Ge, // >=
    }
}
