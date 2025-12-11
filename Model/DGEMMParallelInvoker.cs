using System.Runtime.CompilerServices;

namespace DGEMMSharp.Model;

public partial class DGEMM
{
    internal readonly struct DGEMMParallelInvoker(int mc, int nc, int k, int mr, int nr,
        ReadOnlyMemory<double> aMem, ReadOnlyMemory<double> bMem,
        double beta,
        Memory<double> cMem,
        int ldc) : IAction
    {
        readonly int mc = mc;
        readonly int nc = nc;
        readonly int k = k;
        readonly int mr = mr;
        readonly int nr = nr;
        readonly ReadOnlyMemory<double> aMem = aMem;
        readonly ReadOnlyMemory<double> bMem = bMem;
        readonly double beta = beta;
        readonly Memory<double> cMem = cMem;
        readonly int ldc = ldc;

        public void Invoke(int iIndex)
        {
            Span<double> cBuffer = stackalloc double[mr * nr];
            ref double cBufHead = ref cBuffer[0];
            int i = iIndex * mr;
            int m = Math.Min(mc - i, mr);
            var aSpan = aMem.Span;
            var bSpan = bMem.Span;
            var cSpan = cMem.Span;
            ref double aRef = ref Unsafe.AsRef(in aSpan[i * k]);
            ref double bRef = ref Unsafe.AsRef(in bSpan[0]);
            ref double cRef = ref cSpan[i * ldc];
            for (int j = 0; j < nc; j += nr)
            {
                int n = Math.Min(nc - j, nr);
                LoadFromMatrixC(m, n, ref cBufHead, ref cRef, ldc);
                MicroKernelFunc(k, ref aRef, ref bRef, ref cBufHead, nr);
                StoreBackMatrixC(m, n, ref cBufHead, ref cRef, ldc);
                bRef = ref Unsafe.Add(ref bRef, nr * k);
                cRef = ref Unsafe.Add(ref cRef, nr);
            }
        }
    }
}
