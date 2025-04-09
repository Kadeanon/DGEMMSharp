using BenchmarkDotNet.Configs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using DGEMMSharp.Benchmark;

[assembly: Config(typeof(MyConfig))]

namespace DGEMMSharp.Benchmark
{
    internal class MyConfig : ManualConfig
    {

        public MyConfig()
        {
            AddColumn(new GFlopsColumn());
        }
    }
}
