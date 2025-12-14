using Sigil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace DGEMMSharp.Model.KernelIR.Values;

public abstract class Number(KernelDef kernel, string name) 
    : Value(kernel, name), IVariable<int>
{
    public abstract void LoadValue();

    public abstract void LoadAddr();

    public abstract void StoreValue();
}

public class LocalNumber(KernelDef kernel, string name) 
    : Number(kernel, name)
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

public class ParamNumber(KernelDef kernel, 
    ushort index, string name) 
    : Number(kernel, name)
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
