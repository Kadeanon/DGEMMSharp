using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Attributes;
using DGEMMSharp.Benchmark;

[assembly: Config(typeof(MyConfig))]

namespace DGEMMSharp.Benchmark;

internal class MyConfig : ManualConfig
{

    public MyConfig()
    {
        AddColumn(new GFlopsColumn());
    }
}
