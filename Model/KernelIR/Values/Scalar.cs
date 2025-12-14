using Sigil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DGEMMSharp.Model.KernelIR.Values;

public abstract class Scalar(KernelDef kernel, string name)
    : Value(kernel, name), IVariable<double>
{
    public abstract void LoadValue();

    public abstract void LoadAddr();

    public abstract void StoreValue();
}

public class LocalScalar(KernelDef kernel, string name)
    : Scalar(kernel, name)
{
    public Local Local { get; } = kernel.DefVariable<int>(name);

    public override void LoadValue()
    {
        Emitter.LoadLocal(Local);
    }

    public override void LoadAddr()
    {
        Emitter.LoadLocalAddress(Local);
    }

    public override void StoreValue()
    {
        Emitter.StoreLocal(Local);
    }
}

public class ParamScalar(KernelDef kernel, 
    ushort index, string name) 
    : Scalar(kernel, name)
{
    ushort Index { get; } = index;

    public override void LoadValue()
    {
        Emitter.LoadArgument(Index);
    }

    public override void LoadAddr()
    {
        Emitter.LoadArgumentAddress(Index);
    }

    public override void StoreValue()
    {
        Emitter.StoreArgument(Index);
    }
}

