﻿using AutoGEMM.Benchmark;
using BenchmarkDotNet.Attributes;
using DGEMMSharp.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace DGEMMSharp.Benchmark.Zen2
{
    /// <summary>
    /// It is tested with Amd Ryzen 5 4600H in Windows 10.
    /// </summary>
    public abstract class TestZen2 : BenchmarkBase
    {

        [Benchmark]
        public void Auto()
        {
            DGEMM.GEMM(
                M, N, K, ArrayA, K, ArrayB, N, ArrayC, N);
        }
    }
}
