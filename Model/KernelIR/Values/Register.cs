using Sigil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Intrinsics.X86;
using System.Text;
using System.Threading.Tasks;

namespace DGEMMSharp.Model.KernelIR.Values;

public class Register: Value
{
    public Local Local { get; }

    public Register(KernelDef kernel) : base(kernel)
    {
        Local = kernel.DefRegister();
    }

    public void Load(MemRef refDef)
    {
        refDef.LoadAddr();
        Emitter.Call(Config.LoadVector, null);
        Emitter.StoreLocal(Local);
    }

    public void LoadWithOffset(MemRef refDef, int offset)
    {
        refDef.LoadAddr();
        Emitter.LoadConstant(offset);
        Emitter.Convert<nint>();
        Emitter.Call(Config.LoadVectorWithOffset, null);
        Emitter.StoreLocal(Local);
    }

    public void Broadcast(MemRef refDef)
    {
        refDef.LoadValue();
        Emitter.Call(Config.BroadcastVector, null);
        Emitter.StoreLocal(Local);
    }

    public void UpdateFMA(Register x, Register y)
    {
        Emitter.LoadLocal(x.Local);
        Emitter.LoadLocal(y.Local);
        Emitter.LoadLocal(Local);
        Emitter.Call(Config.MultiAdd, null);
        Emitter.StoreLocal(Local);
    }

    public void Store(MemRef refDef)
    {
        Emitter.LoadLocal(Local);
        refDef.LoadAddr();
        Emitter.Call(Config.StoreVector, null);
    }
}
