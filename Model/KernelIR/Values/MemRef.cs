using Sigil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DGEMMSharp.Model.KernelIR.Values;


public abstract class MemRef(KernelDef def): Value(def), IVariable
{
    public abstract void LoadAddr();

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

    public abstract void StoreAddr();

    public void UpdateRef(int offset)
        => SetByRefAndOffset(this, offset);

    public void IncRef() => SetByRefAndOffset(this, 1);

    public void SetBy(MemRef src)
    {
        src.LoadAddr();
        StoreAddr();
    }

    public void SetByRefAndOffset(MemRef src, int offset)
    {
        src.LoadAddr();
        Emitter.LoadConstant(offset);
        Emitter.Call(ILUtils.AddRefDouble, null);
        StoreAddr();
    }

    public void SetByRefAndOffset(MemRef src, int step, Number offset)
    {
        src.LoadAddr();
        Emitter.LoadConstant(step);
        offset.LoadValue();
        Emitter.Multiply();
        Emitter.Call(ILUtils.AddRefDouble, null);
        StoreAddr();
    }
}

public class LocalMemRef(KernelDef kernel) : MemRef(kernel)
{
    public Local Local { get; } = kernel.DefRef<double>();

    public override void LoadAddr()
    {
        Emitter.LoadLocal(Local);
    }

    public override void StoreAddr()
    {
        Emitter.StoreLocal(Local);
    }
}
public class ParamMemRef(KernelDef def, ushort index) : MemRef(def)
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
