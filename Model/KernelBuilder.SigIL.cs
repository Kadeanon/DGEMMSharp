using DGEMMSharp.Model.KernelIR;
using Microsoft.Diagnostics.Runtime;
using Sigil;
using System.Numerics;
using System.Reflection;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

//using static DGEMMSharp.Model.DGEMM;

using Kernel = DGEMMSharp.Model.DGEMM.Kernel;

namespace DGEMMSharp.Model;

public class SigILEmitter
{
    internal RuntimeConfig Config { get; }

    public SigILEmitter()
    {
        Config = new RuntimeConfig(DGEMM.VectorLevel);
    }

    /// <summary>
    /// Dynamically generates an optimized microkernel using IL code emission.
    /// </summary>
    /// <remarks>
    /// 1. Creates vector registers for C matrix blocks<br />
    /// 2. Generates FMA instructions for vector operations<br />
    /// 3. Automatically adapts to available SIMD capabilities<br />
    /// Uses System.Reflection.Emit to generate hardware-specific optimizations at runtime.
    /// </remarks>
    internal Kernel BuildKernel(int mr, int nr)
    {
        var def = new KernelDef(mr, nr, Config);
        return def.BuildKernel();
    }
}
