// See https://aka.ms/new-console-template for more information
using DGEMMSharp.Benchmark.Zen2;

#if DEBUG
TestZen2 test = new()
{
    Length = 1024 * 8
};
test.CheckDebug(test.Auto);
#else
BenchmarkDotNet.Running.BenchmarkRunner.Run<TestZen2>();
#endif