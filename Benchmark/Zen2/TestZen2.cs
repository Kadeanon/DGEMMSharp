using BenchmarkDotNet.Attributes;
using DGEMMSharp.Model;

namespace DGEMMSharp.Benchmark.Zen2;

/// <summary>
/// It is tested with Amd Ryzen 5 4600H in Windows 10.
/// </summary>
public abstract class TestZen2 : BenchmarkBase
{

    [Benchmark]
    public override void Auto()
    {
        DGEMM.GEMM(
            M, N, K, ArrayA, K, ArrayB, N, ArrayC, N);
    }
}
