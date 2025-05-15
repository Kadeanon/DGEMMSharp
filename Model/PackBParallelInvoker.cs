using CommunityToolkit.HighPerformance;
using CommunityToolkit.HighPerformance.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace DGEMMSharp.Model
{
    public partial class DGEMM
    {
        internal readonly struct PackBParallelInvoker(int kc, int nc, int nr, ReadOnlyMemory<double> bMem, int ldb, double[] bTo) : IAction
        {
            readonly int kc = kc;
            readonly int nc = nc;
            readonly int nr = nr;
            readonly ReadOnlyMemory<double> bMem = bMem;
            readonly int ldb = ldb;
            readonly double[] bTo = bTo;

            public void Invoke(int jIndex)
            {
                int j = jIndex * nr;
                ref double bRef = ref bMem.Span.DangerousGetReferenceAt(j);
                ref double bToRef = ref bTo[kc * j];
                int jc = Math.Min(nc - j, nr);
                if (jc != nr)
                {
                    int last = jc;
                    for (int qc = 0; qc < kc; qc++)
                    {
                        Span<double> span = MemoryMarshal.CreateSpan(ref bToRef, nr);
                        MemoryMarshal.CreateReadOnlySpan(ref bRef, last)
                            .CopyTo(span);
                        span[last..].Clear();
                        bRef = ref Unsafe.Add(ref bRef, ldb);
                        bToRef = ref Unsafe.Add(ref bToRef, nr);
                    }
                }
                else
                {
                    for (int qc = 0; qc < kc; qc++)
                    {
                        // Can little span copying be automatically accelerated by SIMD?
                        MemoryMarshal.CreateReadOnlySpan(ref bRef, nr)
                            .CopyTo(MemoryMarshal.CreateSpan(ref bToRef, nr));
                        bRef = ref Unsafe.Add(ref bRef, ldb);
                        bToRef = ref Unsafe.Add(ref bToRef, nr);
                    }
                }
            }
        }
    }
}
