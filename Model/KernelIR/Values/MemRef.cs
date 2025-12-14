using Sigil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DGEMMSharp.Model.KernelIR.Values;


public abstract class MemRef(KernelDef def, string name)
    : Value(def, name), IVariable<double>
{
    public abstract void LoadAddr();

    public abstract void StoreAddr();

    public void LoadValue()
    {
        LoadAddr();
        Emitter.LoadIndirect<double>();
    }

    public void StoreValue()
    {
        LoadAddr();
        Emitter.StoreIndirect<double>();
    }
}

public class LocalMemRef(KernelDef kernel, string name) 
    : MemRef(kernel, name)
{
    public Local Local { get; } = kernel.DefRef<double>(name);

    public override void LoadAddr()
    {
        Emitter.LoadLocal(Local);
    }

    public override void StoreAddr()
    {
        Emitter.StoreLocal(Local);
    }
}
public class ParamMemRef(KernelDef def, ushort index, string name)
    : MemRef(def, name)
{
    ushort Index { get; } = index;

    public override void LoadAddr()
    {
        Emitter.LoadArgument(Index);
    }

    public override void StoreAddr()
    {
        Emitter.StoreArgument(Index);
    }
}
