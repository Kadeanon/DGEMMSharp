using AutoGEMM.Benchmark;
using BenchmarkDotNet.Attributes;
using DGEMMSharp.Model;

namespace DGEMMSharp.Benchmark.Zen2;

public class TestZen2Parallel : BenchmarkBase
{
    public override IEnumerable<int> TestValues()
    {
        yield return 512;
        yield return 1024 * 2;
        yield return 1024 * 8;
    }

    [Benchmark]
    public void Auto()
    {
        DGEMM.GEMM(
            M, N, K, ArrayA, K, ArrayB, N, ArrayC, N);
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
