using Sigil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace DGEMMSharp.Model.KernelIR.Values;

public abstract class Number(KernelDef kernel) : Value(kernel), IVariable
{
    public abstract void LoadValue();

    public abstract void LoadAddr();

    public abstract void StoreValue();

    public void Set(int value)
    {
        Emitter.LoadConstant(value);
        StoreValue();
    }

    public void Inc()
    {
        LoadValue();
        Emitter.LoadConstant(1);
        Emitter.Add();
        StoreValue();
    }
}

public class LocalNumber(KernelDef kernel) : Number(kernel)
{
    public Local Local { get; } = kernel.DefVariable<int>();

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

public class ParamNumber(KernelDef kernel, ushort index) : Number(kernel)
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
