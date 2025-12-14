using Sigil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Intrinsics.X86;
using System.Text;
using System.Threading.Tasks;

namespace DGEMMSharp.Model.KernelIR.Values;

public class Register(KernelDef kernel, string name)
    : Value(kernel, name), ISrc, IDest
{
    public Local Local { get; } = kernel.DefRegister(name);

    public void LoadValue()
    {
        Emitter.LoadLocal(Local);
    }

    public void StoreValue()
    {
        Emitter.StoreLocal(Local);
    }
}
