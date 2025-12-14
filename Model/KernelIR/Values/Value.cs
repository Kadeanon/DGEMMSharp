using Sigil;

namespace DGEMMSharp.Model.KernelIR.Values;

public interface IValue
{
    public KernelDef Def { get; }

    public string Name { get; }

    public Emit<DGEMM.Kernel> Emitter => Def.Emitter;

    public RuntimeConfig Config => Def.Config;

}

public abstract class Value(KernelDef def, string name)
{
    public KernelDef Def { get; } = def;

    public string Name { get; } = name;

    public Emit<DGEMM.Kernel> Emitter => Def.Emitter;

    public RuntimeConfig Config => Def.Config;

    public override string ToString() => Name;
}
