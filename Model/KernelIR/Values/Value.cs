using Sigil;

namespace DGEMMSharp.Model.KernelIR.Values;

public abstract class Value(KernelDef def)
{
    public KernelDef Def { get; } = def;

    public Emit<DGEMM.Kernel> Emitter => Def.Emitter;

    public RuntimeConfig Config => Def.Config;
}
