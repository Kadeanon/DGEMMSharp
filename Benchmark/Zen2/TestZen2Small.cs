﻿using AutoGEMM.Benchmark;
using BenchmarkDotNet.Attributes;
using DGEMMSharp.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DGEMMSharp.Benchmark.Zen2
{
    public class TestZen2Small : BenchmarkBase
    {
        public override IEnumerable<int> TestValues()
        {
            yield return 4;
            yield return 16;
            yield return 32;
            yield return 64;
            yield return 128;
            yield return 200;
            yield return 256;
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
}
