// See https://aka.ms/new-console-template for more information
using DGEMMSharp.Benchmark.Zen2;

#if DEBUG
TestZen2Parallel test = new() { Length = 4096 };
test.CheckDebug(test.Auto);
test = new() { Length = 64 };
test.CheckDebug(test.Auto);
#else
var switcher = BenchmarkDotNet.Running.BenchmarkSwitcher.FromTypes([
    typeof(TestZen2Small),
    typeof(TestZen2Sequence),
    typeof(TestZen2Parallel)]);
switcher.Run();
#endif