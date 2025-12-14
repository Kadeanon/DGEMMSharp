using BenchmarkDotNet.Attributes;
using DGEMMSharp.Model;
using System.Diagnostics;

namespace DGEMMSharp.Benchmark.Zen2;

[DisassemblyDiagnoser]
public class TestZen2Parallel : TestZen2
{
    public override IEnumerable<int> TestValues()
    {
        yield return 512;
        yield return 1024 * 2;
        yield return 1024 * 4;
    }

    [Benchmark]
    public unsafe void MKL()
    {
        BlasHelpers.MKLDgemm(
            M, N, K, ArrayA, K, ArrayB, N, ArrayC, N);
    }

    [Benchmark(Baseline = true)]
    public unsafe void OpenBlas()
    {
        BlasHelpers.OpenBlasDgemm(
            M, N, K, ArrayA, K, ArrayB, N, ArrayC, N);
    }
}
