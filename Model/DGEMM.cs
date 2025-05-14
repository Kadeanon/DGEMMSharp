using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Text;
using System.Threading.Tasks;

namespace DGEMMSharp.Model
{
    public static class DGEMM
    {
        #region parameters
        // Adjust these parameters to determine the optimal packing/kernel form.

        // mr and nr determines the microkernel, so whenever they are modified, 
        // the MicroKernelFunc should be rebuilt.
        private static int mr = 4;
        private static int nr = 8;

        // these three params is used to determine the macro kernel and packing subroutine.
        // Adjust them to improve cache hit rate and enhance function execution efficiency.
        public const int mc = 1024;
        public const int kc = 1024;
        public const int nc = 256;


        internal static VectorType VectorLevel { get; }
        public static Type StaticSIMDType { get; }
        public static Type SIMDType { get; }
        #endregion

        static DGEMM()
        {
            // Now only Vector256 is tested, so other vector level may not work.
            if (Vector512.IsHardwareAccelerated)
            {
                VectorLevel = VectorType.Vector512;
                StaticSIMDType = typeof(Vector512);
                SIMDType = typeof(Vector512<double>);
            }
            if (Vector256.IsHardwareAccelerated)
            {
                VectorLevel = VectorType.Vector256;
                StaticSIMDType = typeof(Vector256);
                SIMDType = typeof(Vector256<double>);
            }
            else if (Vector128.IsHardwareAccelerated)
            {
                VectorLevel = VectorType.Vector128;
                StaticSIMDType = typeof(Vector128);
                SIMDType = typeof(Vector128<double>);
            }
            else
            {
                VectorLevel = VectorType.Scalar;
                throw new NotSupportedException("Not supported yet!");
            }
            BuildKernel();
        }

        #region DGEMM

        /// <summary>
        /// Simplified Mtarix Multify: C = A * B
        /// </summary>
        /// <summary>
        /// Performs the simplified matrix multiplication operation: C = AB + C
        /// </summary>
        /// <param name="m">Number of rows in matrix A.</param>
        /// <param name="n">Number of columns in matrix B.</param>
        /// <param name="k">Number of columns in A / rows in B.</param>
        /// <param name="a">Input matrix A (row-major order).</param>
        /// <param name="lda">Leading dimension of matrix A (typically equal to columns).</param>
        /// <param name="b">Input matrix B (row-major order).</param>
        /// <param name="ldb">Leading dimension of matrix B.</param>
        /// <param name="c">Output matrix C (row-major order).</param>
        /// <param name="ldc">Leading dimension of matrix C.</param>
        public static void GEMM(int m, int n, int k,
            ReadOnlySpan<double> a, int lda,
            ReadOnlySpan<double> b, int ldb,
            Span<double> c, int ldc)
            => GEMM(m, n, k, alpha: 1, a, lda, b, ldb, beta: 0, c, ldc);

        /// <summary>
        /// Simplified Mtarix Multify: C = A * B
        /// </summary>
        /// <summary>
        /// Performs the simplified matrix multiplication operation: C = AB + C
        /// </summary>
        /// <param name="m">Number of rows in matrix A.</param>
        /// <param name="n">Number of columns in matrix B.</param>
        /// <param name="k">Number of columns in A / rows in B.</param>
        /// <param name="a">Input matrix A (row-major order).</param>
        /// <param name="lda">Leading dimension of matrix A (typically equal to columns).</param>
        /// <param name="b">Input matrix B (row-major order).</param>
        /// <param name="ldb">Leading dimension of matrix B.</param>
        /// <param name="c">Output matrix C (row-major order).</param>
        /// <param name="ldc">Leading dimension of matrix C.</param>
        public static void GEMM(int m, int n, int k, 
            double alpha,
            ReadOnlySpan<double> a, int lda,
            ReadOnlySpan<double> b, int ldb,
            double beta,
            Span<double> c, int ldc)
        {
            int aLength = a.Length;
            ArgumentOutOfRangeException.ThrowIfLessThan(aLength, m * lda, nameof(a));
            ref double aHead = ref Unsafe.AsRef(in a[0]);
            int bLength = b.Length;
            ArgumentOutOfRangeException.ThrowIfLessThan(bLength, k * ldb, nameof(b));
            ref double bHead = ref Unsafe.AsRef(in b[0]);
            int cLength = c.Length;
            ArgumentOutOfRangeException.ThrowIfLessThan(cLength, m * ldc, nameof(c));
            ref double cHead = ref Unsafe.AsRef(in c[0]);

            GEMM(m, n, k,
                alpha,
                ref aHead, lda,
                ref bHead, ldb,
                beta,
                ref cHead, ldc);
        }

        /// <summary>
        /// Simplified Mtarix Multify: C += A * B
        /// </summary>
        /// <param name="m">the rows of matrix a</param>
        /// <param name="n">the columns of matrix b</param>
        /// <param name="k">the columns of matirx a and the rows of matrix b</param>
        /// <param name="a">the managed pointer of data for matrix a, row-major</param>
        /// <param name="lda">the leading dimension of matrix a </param>
        /// <param name="b">the managed pointer of data for matrix b, row-major</param>
        /// <param name="ldb">the leading dimension of matrix b</param>
        /// <param name="c">the managed pointer of data for matrix c, row-major</param>
        /// <param name="ldc">the leading dimension of matrix a</param>
        internal static void GEMM(int m, int n, int k,
            double alpha,
            ref double a, int lda,
            ref double b, int ldb,
            double beta,
            ref double c, int ldc)
        {
            int mc = DGEMM.mc;
            int nc = DGEMM.nc;
            int kc = DGEMM.kc;
            ScalMatrixC(m, n, ref c, ldc, beta);
            double[] packedA = ArrayPool<double>.Shared.Rent(mc * kc);
            double[] packedB = ArrayPool<double>.Shared.Rent(kc * nc);
            ref double packedARef = ref packedA[0];
            ref double packedBRef = ref packedB[0];

            int i;
            for (i = 0; i < m; i += mc)
            {
                int mc2 = Math.Min(m - i, mc);
                int q;
                for (q = 0; q < k; q += kc)
                {
                    int kc2 = Math.Min(k - q, kc);
                    PackMatrixA(mc2, kc2, ref Unsafe.Add(ref a, i * lda + q), lda, ref packedARef, alpha);
                    int j;
                    for (j = 0; j < n; j += nc)
                    {
                        int nc2 = Math.Min(n - j, nc);
                        PackMatrixB(kc2, nc2, ref Unsafe.Add(ref b, q * ldb + j), ldb, ref packedBRef);
                        MacroKernel(mc2, nc2, kc2,
                            ref packedARef,
                            ref packedBRef,
                            beta,
                            ref Unsafe.Add(ref c, i * ldc + j), ldc);
                    }
                }
            }

            ArrayPool<double>.Shared.Return(packedA, clearArray: true);
            ArrayPool<double>.Shared.Return(packedB, clearArray: true);
        }

        /// <summary>
        /// Macro kernel that processes blocked matrix multiplication.
        /// </summary>
        /// <param name="mc">Row block size of matrix A.</param>
        /// <param name="nc">Column block size of matrix B.</param>
        /// <param name="k">Common dimension size.</param>
        /// <param name="a">Packed block of matrix A.</param>
        /// <param name="b">Packed block of matrix B.</param>
        /// <param name="c">Reference to output matrix C.</param>
        /// <param name="ldc">Leading dimension of matrix C.</param>
        /// <remarks>
        /// Organizes computation into smaller blocks (mr x nr) for the microkernel.
        /// Iterates over the packed blocks and dispatches to the vectorized microkernel.
        /// </remarks>
        internal static void MacroKernel(int mc, int nc, int k,
            ref double a,
            ref double b, 
            double beta,
            ref double c, int ldc)
        {
            int mr = DGEMM.mr;
            int nr = DGEMM.nr;
            Span<double> cBuffer = stackalloc double[mr * nr];

            ref double aRef = ref a;
            ref double bRef = ref b;
            ref double cRef = ref c;
            ref double cBufHead = ref cBuffer[0];
            for (int i = 0; i < mc; i += mr)
            {
                int m = Math.Min(mc - i, mr);
                bRef = ref b;
                cRef = ref Unsafe.Add(ref c, i * ldc);
                for (int j = 0; j < nc; j += nr)
                {
                    int n = Math.Min(nc - j, nr);
                    LoadFromMatrixC(m, n, ref cBufHead, ref cRef, ldc);
                    MicroKernelFunc(k, ref aRef, ref bRef, ref cBufHead, nr);
                    StoreBackMatrixC(m, n, ref cBufHead, ref cRef, ldc);
                    bRef = ref Unsafe.Add(ref bRef, nr * k);
                    cRef = ref Unsafe.Add(ref cRef, nr);
                }
                aRef = ref Unsafe.Add(ref aRef, mr * k);
            }
        }

        /// <summary>
        /// Vectorized microkernel using SIMD instructions for fused multiply-add (FMA) operations.
        /// It is a example method.
        /// </summary>
        /// <param name="k">Common dimension size.</param>
        /// <param name="a">Reference to current A sub-block.</param>
        /// <param name="b">Reference to current B sub-block.</param>
        /// <param name="c">Reference to current C sub-block.</param>
        /// <param name="ldc">Leading dimension of matrix C.</param>
        /// <remarks>
        /// 1. Loads 4x8 block of C into vector registers
        /// 2. Performs outer product computation using FMA instructions
        /// 3. Writes results back to memory
        /// Uses Vector256<double> for AVX2 optimization (4 double elements per vector).
        /// </remarks>
        internal static void MicroKernel(int k,
            ref double a,
            ref double b,
            ref double c, int ldc)
        {
            #region LoadVectorC
            ref double c0 = ref c;
            ref double c1 = ref Unsafe.Add(ref c0, 4);

            Span<double> cSpan00 = MemoryMarshal.CreateSpan(ref c, 4);
            Vector256<double> cVec00 = Vector256.Create<double>(cSpan00);
            c = ref Unsafe.Add(ref c, ldc);
            Span<double> cSpan01 = MemoryMarshal.CreateSpan(ref c1, 4);
            Vector256<double> cVec01 = Vector256.Create<double>(cSpan01);
            c1 = ref Unsafe.Add(ref c1, ldc);
            Span<double> cSpan10 = MemoryMarshal.CreateSpan(ref c, 4);
            Vector256<double> cVec10 = Vector256.Create<double>(cSpan10);
            c = ref Unsafe.Add(ref c, ldc);
            Span<double> cSpan11 = MemoryMarshal.CreateSpan(ref c1, 4);
            Vector256<double> cVec11 = Vector256.Create<double>(cSpan11);
            c1 = ref Unsafe.Add(ref c1, ldc);
            Span<double> cSpan20 = MemoryMarshal.CreateSpan(ref c, 4);
            Vector256<double> cVec20 = Vector256.Create<double>(cSpan20);
            c = ref Unsafe.Add(ref c, ldc);
            Span<double> cSpan21 = MemoryMarshal.CreateSpan(ref c1, 4);
            Vector256<double> cVec21 = Vector256.Create<double>(cSpan21);
            c1 = ref Unsafe.Add(ref c1, ldc);
            Span<double> cSpan30 = MemoryMarshal.CreateSpan(ref c, 4);
            Vector256<double> cVec30 = Vector256.Create<double>(cSpan30);
            c = ref Unsafe.Add(ref c, ldc);
            Span<double> cSpan31 = MemoryMarshal.CreateSpan(ref c1, 4);
            Vector256<double> cVec31 = Vector256.Create<double>(cSpan31);
            c1 = ref Unsafe.Add(ref c1, ldc);

            #endregion

            #region CycleFMA
            ref double aRef = ref a;
            ref double bRef = ref b;
            for (int p = 0; p < k; p++)
            {
                Vector256<double> bVec0 = Vector256.Create<double>(MemoryMarshal.CreateSpan(ref bRef, 4));
                bRef = ref Unsafe.Add(ref bRef, 4);
                Vector256<double> bVec1 = Vector256.Create<double>(MemoryMarshal.CreateSpan(ref bRef, 4));
                bRef = ref Unsafe.Add(ref bRef, 4);

                Vector256<double> aVex0 = Vector256.Create(aRef);
                aRef = ref Unsafe.Add(ref aRef, 1);
                Vector256<double> aVec1 = Vector256.Create(aRef);
                aRef = ref Unsafe.Add(ref aRef, 1);
                Vector256<double> aVec2 = Vector256.Create(aRef);
                aRef = ref Unsafe.Add(ref aRef, 1);
                Vector256<double> aVec3 = Vector256.Create(aRef);
                aRef = ref Unsafe.Add(ref aRef, 1);

                cVec00 += aVex0 * bVec0;
                cVec01 += aVex0 * bVec1;
                cVec10 += aVec1 * bVec0;
                cVec11 += aVec1 * bVec1;
                cVec20 += aVec2 * bVec0;
                cVec21 += aVec2 * bVec1;
                cVec30 += aVec3 * bVec0;
                cVec31 += aVec3 * bVec1;
            }
            #endregion

            #region StoreVectorC
            cVec00.CopyTo(cSpan00);
            cVec01.CopyTo(cSpan01);
            cVec10.CopyTo(cSpan10);
            cVec11.CopyTo(cSpan11);
            cVec20.CopyTo(cSpan20);
            cVec21.CopyTo(cSpan21);
            cVec30.CopyTo(cSpan30);
            cVec31.CopyTo(cSpan31);
            #endregion
        }

        /// <summary>
        /// Packs a block of matrix A into contiguous memory for better cache utilization.
        /// </summary>
        /// <param name="mc">Row block size.</param>
        /// <param name="kc">Column block size.</param>
        /// <param name="a">Source matrix A reference.</param>
        /// <param name="lda">Leading dimension of A.</param>
        /// <param name="aTo">Target packed memory location.</param>
        /// <remarks>
        /// Reorganizes data from row-major to a blocked layout to enable sequential memory access
        /// during microkernel computation. Reduces cache misses by ensuring temporal locality.
        /// </remarks>
        private static void PackMatrixA(int mc, int kc, ref double a, int lda, ref double aTo, double alpha)
        {
            ref double aPtr = ref a;
            ref double aToRef = ref aTo;
            int last = mc % mr; 
            int ir = 0;
            for (; ir <= mc - mr; ir += mr)
            {
                aPtr = ref Unsafe.Add(ref a, ir * lda);
                for (int qc = 0; qc < kc; qc++)
                {
                    ref double aRef = ref aPtr;
                    for (int i = 0; i < mr; i++)
                    {
                        aToRef = aRef * alpha;
                        aRef = ref Unsafe.Add(ref aRef, lda);
                        aToRef = ref Unsafe.Add(ref aToRef, 1);
                    }
                    aPtr = ref Unsafe.Add(ref aPtr, 1);
                }
            }
            if(last > 0)
            {
                aPtr = ref Unsafe.Add(ref a, ir * lda);
                for (int qc = 0; qc < kc; qc++)
                {
                    ref double aRef = ref aPtr;
                    int i = 0;
                    for (; i < last; i++)
                    {
                        aToRef = aRef * alpha;
                        aRef = ref Unsafe.Add(ref aRef, lda);
                        aToRef = ref Unsafe.Add(ref aToRef, 1);
                    }
                    for (; i < mr; i++)
                    {
                        aToRef = 0;
                        aToRef = ref Unsafe.Add(ref aToRef, 1);
                    }
                    aPtr = ref Unsafe.Add(ref aPtr, 1);
                }
            }
        }

        /// <summary>
        /// Packs a block of matrix B into contiguous memory for better cache utilization.
        /// </summary>
        /// <param name="nc">Row block size.</param>
        /// <param name="kc">Column block size.</param>
        /// <param name="b">Source matrix B reference.</param>
        /// <param name="ldb">Leading dimension of B.</param>
        /// <param name="bTo">Target packed memory location.</param>
        /// <remarks>
        private static void PackMatrixB(int kc, int nc, ref double b, int ldb, ref double bTo)
        {
            ref double bPtr = ref b;
            ref double bToRef = ref bTo;
            int last = mc % mr;
            int jr = 0;
            for (; jr <= nc - nr; jr += nr)
            {
                bPtr = ref Unsafe.Add(ref b, jr);
                for (int qc = 0; qc < kc; qc++)
                {
                    ref double bRef = ref bPtr;
                    // Can little span copying be automatically accelerated by SIMD?
                    MemoryMarshal.CreateReadOnlySpan(ref bPtr, nr)
                        .CopyTo(MemoryMarshal.CreateSpan(ref bToRef, nr));
                    bPtr = ref Unsafe.Add(ref bPtr, ldb);
                    bToRef = ref Unsafe.Add(ref bToRef, nr);
                }
            }
            if(last > 0)
            {
                bPtr = ref Unsafe.Add(ref b, jr);
                for (int qc = 0; qc < kc; qc++)
                {
                    Span<double> span = MemoryMarshal.CreateSpan(ref bToRef, nr);
                    MemoryMarshal.CreateReadOnlySpan(ref bPtr, last)
                        .CopyTo(span);
                    span[last..].Clear();
                    bPtr = ref Unsafe.Add(ref bPtr, ldb);
                    bToRef = ref Unsafe.Add(ref bToRef, nr);
                }
            }
        }

        private static void ScalMatrixC(int mc, int nc, ref double c, int ldc, double beta)
        {
            //Here simply use Vector to accelerate the process.
            bool simd = Vector.IsHardwareAccelerated;
            //If beta is 0, dirctly fill c to 0.
            bool zero = beta == 0;
            //use common simd with loop unrolling.
            if (simd)
            {
                int vecSize = Vector<double>.Count;
                int last = nc % vecSize;
                Vector<double> betaVec = Vector.Create(beta);
                ref double cPtr = ref c;
                if (zero)
                {
                    for (int i = 0; i < mc; i++)
                    {
                        ref double cRef = ref cPtr;
                        int j = 0;
                        for (; j <= mc - vecSize; j += vecSize)
                        {
                            betaVec.StoreUnsafe(ref cRef);
                            cRef = ref Unsafe.Add(ref cRef, vecSize);
                        }
                        for (; j < mc; j++)
                        {
                            cRef = 0.0;
                            cRef = ref Unsafe.Add(ref cRef, 1);
                        }
                        cPtr = ref Unsafe.Add(ref cPtr, ldc);
                    }
                }
                else
                {
                    int unrollSize = vecSize * 4;
                    for (int i = 0; i < mc; i++)
                    {
                        ref double cRef0 = ref cPtr;
                        ref double cRef1 = ref Unsafe.Add(ref cPtr, vecSize);
                        ref double cRef2 = ref Unsafe.Add(ref cPtr, vecSize * 2);
                        ref double cRef3 = ref Unsafe.Add(ref cPtr, vecSize * 3);
                        int j = 0;
                        for (; j <= mc - vecSize * 4; j += vecSize * 4)
                        {
                            Vector<double> cVec0 = Vector.LoadUnsafe(ref cRef0);
                            cVec0 *= betaVec;
                            cVec0.StoreUnsafe(ref cRef0);
                            cRef0 = ref Unsafe.Add(ref cRef0, unrollSize);
                            Vector<double> cVec1 = Vector.LoadUnsafe(ref cRef1);
                            cVec1 *= betaVec;
                            cVec1.StoreUnsafe(ref cRef1);
                            cRef1 = ref Unsafe.Add(ref cRef1, unrollSize);
                            Vector<double> cVec2 = Vector.LoadUnsafe(ref cRef2);
                            cVec2 *= betaVec;
                            cVec2.StoreUnsafe(ref cRef2);
                            cRef2 = ref Unsafe.Add(ref cRef2, unrollSize);
                            Vector<double> cVec3 = Vector.LoadUnsafe(ref cRef3);
                            cVec3 *= betaVec;
                            cVec3.StoreUnsafe(ref cRef3);
                            cRef3 = ref Unsafe.Add(ref cRef3, unrollSize);
                        }
                        for (; j <= mc - vecSize; j += vecSize)
                        {
                            Vector<double> cVec = Vector.LoadUnsafe(ref cRef0);
                            cVec *= betaVec;
                            cVec.StoreUnsafe(ref cRef0);
                            cRef0 = ref Unsafe.Add(ref cRef0, vecSize);
                        }
                        for (; j < mc; j++)
                        {
                            cRef0 *= beta;
                            cRef0 = ref Unsafe.Add(ref cRef0, 1);
                        }
                        cPtr = ref Unsafe.Add(ref cPtr, ldc);
                    }
                }
            }
            else
            {
                int unrollSize = 4;
                ref double cPtr = ref c;
                if (zero)
                {
                    for (int i = 0; i < mc; i++)
                    {
                        MemoryMarshal.CreateSpan(ref cPtr, nc).Clear();
                        cPtr = ref Unsafe.Add(ref cPtr, ldc);
                    }
                }
                else
                {
                    //use *4 loop unrolling.
                    for (int i = 0; i < mc; i++)
                    {
                        ref double cRef = ref cPtr;
                        int j = 0;
                        for (; j <= mc - unrollSize; j += unrollSize)
                        {
                            cRef = beta;
                            cRef = ref Unsafe.Add(ref cRef, 1);
                            cRef *= beta;
                            cRef = ref Unsafe.Add(ref cRef, 1);
                            cRef *= beta;
                            cRef = ref Unsafe.Add(ref cRef, 1);
                            cRef *= beta;
                            cRef = ref Unsafe.Add(ref cRef, 1);
                        }
                        for (; j < mc; j++)
                        {
                            cRef *= beta;
                            cRef = ref Unsafe.Add(ref cRef, 1);
                        }
                        cPtr = ref Unsafe.Add(ref cPtr, ldc);
                    }
                }
            }
        }

        private static void LoadFromMatrixC(int m, int n, ref double cBufferHead, ref double c, int ldc)
        {
            bool simd = Vector.IsHardwareAccelerated;
            int ldcBuffer = nr;
            int cBufferLines = mr;
            if (simd)
            {
                int vecSize = Vector256<double>.Count;
                ref double cBufferRef = ref cBufferHead;
                int i = 0;
                Vector<double> zeroVec = Vector<double>.Zero;
                for (; i < cBufferLines; i++)
                {
                    int nTemp = n;
                    if (i >= m) nTemp = 0;
                    int nleft = ldcBuffer - nTemp;
                    ref double cRef = ref c;
                    int j = 0;
                    for (; j <= nTemp - vecSize; j += vecSize)
                    {
                        Vector<double> vec = Vector.LoadUnsafe(ref cRef);
                        vec.StoreUnsafe(ref cBufferRef);
                        cBufferRef = ref Unsafe.Add(ref cBufferRef, vecSize);
                        cRef = ref Unsafe.Add(ref cRef, vecSize);
                    }
                    for (; j < nTemp; j++)
                    {
                        cBufferRef = cRef;
                        cBufferRef = ref Unsafe.Add(ref cBufferRef, 1);
                        cRef = ref Unsafe.Add(ref cRef, 1);
                    }
                    if(nleft > 0)
                    {
                        j = 0;
                        for (; j <= nleft - vecSize; j += vecSize)
                        {
                            zeroVec.StoreUnsafe(ref cBufferRef);
                            cBufferRef = ref Unsafe.Add(ref cBufferRef, vecSize);
                        }
                        for (; j < nleft; j++)
                        {
                            cBufferRef = 0;
                            cBufferRef = ref Unsafe.Add(ref cBufferRef, 1);
                        }
                    }
                    c = ref Unsafe.Add(ref c, ldc);
                }
            }
            else
            {
                int unrollSize = 4;
                ref double cBufferRef = ref cBufferHead;
                int i = 0;
                for (; i < m; i++)
                {
                    int nTemp = n;
                    if (i > m) nTemp = 0;
                    int nleft = ldcBuffer - n;
                    ref double cRef = ref c;
                    int j = 0;
                    for (; j <= nTemp - unrollSize; j += unrollSize)
                    {
                        cRef = cBufferHead;
                        cBufferRef = ref Unsafe.Add(ref cBufferRef, 1);
                        cRef = ref Unsafe.Add(ref cRef, 1);
                        cRef = cBufferHead;
                        cBufferRef = ref Unsafe.Add(ref cBufferRef, 1);
                        cRef = ref Unsafe.Add(ref cRef, 1);
                        cRef = cBufferHead;
                        cBufferRef = ref Unsafe.Add(ref cBufferRef, 1);
                        cRef = ref Unsafe.Add(ref cRef, 1);
                        cRef = cBufferHead;
                        cBufferRef = ref Unsafe.Add(ref cBufferRef, 1);
                        cRef = ref Unsafe.Add(ref cRef, 1);
                    }
                    for (; j < nTemp; j++)
                    {
                        cRef = cBufferHead;
                        cBufferRef = ref Unsafe.Add(ref cBufferRef, 1);
                        cRef = ref Unsafe.Add(ref cRef, 1);
                    }
                    if (nleft > 0)
                    {
                        j = 0;
                        for (; j <= nleft - unrollSize; j += unrollSize)
                        {
                            cBufferRef = 0;
                            Unsafe.Add(ref cBufferRef, 1) = 0;
                            Unsafe.Add(ref cBufferRef, 2) = 0;
                            Unsafe.Add(ref cBufferRef, 3) = 0;
                            cBufferRef = ref Unsafe.Add(ref cBufferRef, 1);
                        }
                        for (; j < nleft; j++)
                        {
                            cBufferRef = 0;
                            cBufferRef = ref Unsafe.Add(ref cBufferRef, 1);
                        }
                    }
                    c = ref Unsafe.Add(ref c, ldc);
                    cBufferHead = ref Unsafe.Add(ref cBufferHead, ldcBuffer);
                }
            }
        }

        private static void StoreBackMatrixC(int m, int n, ref double cBufferHead, ref double c, int ldc)
        {
            bool simd = Vector.IsHardwareAccelerated;
            int ldcBuffer = DGEMM.nr;
            if (simd)
            {
                int vecSize = Vector256<double>.Count;
                for (int i = 0; i < m; i++)
                {
                    ref double cRef = ref c;
                    ref double cBufferRef = ref cBufferHead;
                    int j = 0;
                    for (; j <= n - vecSize; j += vecSize)
                    {
                        Vector<double> vec = Vector.LoadUnsafe(ref cBufferRef);
                        vec.StoreUnsafe(ref cRef);
                        cBufferRef = ref Unsafe.Add(ref cBufferRef, vecSize);
                        cRef = ref Unsafe.Add(ref cRef, vecSize);
                    }
                    for (; j < n; j++)
                    {
                        cRef = cBufferHead;
                        cBufferRef = ref Unsafe.Add(ref cBufferRef, 1);
                        cRef = ref Unsafe.Add(ref cRef, 1);
                    }
                    c = ref Unsafe.Add(ref c, ldc);
                    cBufferHead = ref Unsafe.Add(ref cBufferHead, ldcBuffer);
                }
            }
            else
            {
                int unrollSize = 4;
                for (int i = 0; i < m; i++)
                {
                    ref double cRef = ref c;
                    ref double cBufferRef = ref cBufferHead;
                    int j = 0;
                    for (; j <= n - unrollSize; j += unrollSize)
                    {
                        cRef = cBufferHead;
                        cBufferRef = ref Unsafe.Add(ref cBufferRef, 1);
                        cRef = ref Unsafe.Add(ref cRef, 1);
                        cRef = cBufferHead;
                        cBufferRef = ref Unsafe.Add(ref cBufferRef, 1);
                        cRef = ref Unsafe.Add(ref cRef, 1);
                        cRef = cBufferHead;
                        cBufferRef = ref Unsafe.Add(ref cBufferRef, 1);
                        cRef = ref Unsafe.Add(ref cRef, 1);
                        cRef = cBufferHead;
                        cBufferRef = ref Unsafe.Add(ref cBufferRef, 1);
                        cRef = ref Unsafe.Add(ref cRef, 1);
                    }
                    for (; j < n; j++)
                    {
                        cRef = cBufferHead;
                        cBufferRef = ref Unsafe.Add(ref cBufferRef, 1);
                        cRef = ref Unsafe.Add(ref cRef, 1);
                    }
                    c = ref Unsafe.Add(ref c, ldc);
                    cBufferHead = ref Unsafe.Add(ref cBufferHead, ldcBuffer);
                }
            }
        }
        #endregion

        #region Emit Micro Kernal

        public delegate void Kernel(int k,
            ref double a,
            ref double b,
            ref double c, int ldc);

        public static Kernel MicroKernelFunc { get; private set; }

        /// <summary>
        /// Dynamically generates an optimized microkernel using IL code emission.
        /// </summary>
        /// <remarks>
        /// 1. Creates vector registers for C matrix blocks<br />
        /// 2. Generates FMA instructions for vector operations<br />
        /// 3. Automatically adapts to available SIMD capabilities<br />
        /// Uses System.Reflection.Emit to generate hardware-specific optimizations at runtime.
        /// </remarks>
        [MemberNotNullAttribute(nameof(MicroKernelFunc))]
        private static void BuildKernel()
        {
            Type intType = typeof(int);
            Type doubleRefType = typeof(double).MakeByRefType();
            DynamicMethod dynamicKernel = new DynamicMethod("dynamicKernel",
                typeof(void), [intType, doubleRefType, doubleRefType, doubleRefType, intType]);
            ILGenerator il = dynamicKernel.GetILGenerator();

            // Load the parameters onto the evaluation stack
            VectorType vectorType = VectorLevel;
            int nVec = vectorType switch
            {
                VectorType.Vector128 => 2,
                VectorType.Vector256 => 4,
                VectorType.Vector512 => 8,
                _ => throw new NotSupportedException("Unsupported yet")
            };
            int nv = nr / nVec;

            var cVars = il.BuildLoadVectorC(mr, nv, vectorType);
            il.BuildCycleFMA(mr, nv, vectorType, cVars);
            il.BuildStoreVectorC(vectorType, cVars);
            il.Emit(OpCodes.Ret);
            MicroKernelFunc = dynamicKernel.CreateDelegate<Kernel>();
        }

        private static LocalBuilder[] BuildLoadVectorC(this ILGenerator il, int mr, int nv, VectorType vectorType)
        {
            Type doubleType = typeof(double);
            Type intType = typeof(int);
            Type doubleRefType = doubleType.MakeByRefType();

            Type simdType = vectorType switch
            {
                VectorType.Vector128 => typeof(Vector128<double>),
                VectorType.Vector256 => typeof(Vector256<double>),
                VectorType.Vector512 => typeof(Vector512<double>),
                _ => throw new NotSupportedException("Unsupported vector type")
            };
            Type spanType = typeof(Span<double>);
            int vectorSize = vectorType switch
            {
                VectorType.Vector128 => 2,
                VectorType.Vector256 => 4,
                VectorType.Vector512 => 8,
                _ => throw new NotSupportedException("Unsupported vector type")
            };

            MethodInfo addDouble = ILUtils.AddDouble;
            MethodInfo createSpan = ILUtils.CreateSpan;
            MethodInfo createVector = ILUtils.DynamicCreateVectorFromSpan(StaticSIMDType);
            MethodInfo implictConv = ILUtils.ConvSpanAsReadOnly;

            LocalBuilder[] cRefs = new LocalBuilder[nv];
            LocalBuilder[] cVars = new LocalBuilder[mr * nv * 2];
            int numVec = mr * nv;
            int counter = 0;

            // define extra c ref
            for (int i = 0; i < nv; i++)
            {
                var cRefDef = il.DeclareLocal(doubleRefType);
                cRefs[counter++] = cRefDef;
            }

            var cRef = cRefs[0];
            // load the first cRef
            il.Emit(OpCodes.Ldarg_3);
            il.EmitStoreLocal(cRef);
            if (nv > 1)
            {
                // load the rest cRef
                for (int j = 1; j < nv; j++)
                {
                    var cRef2 = cRefs[j];
                    il.EmitAddRef(cRef, vectorSize, cRef2);
                    cRef = cRef2;
                }
            }

            // define the local variables for cSpan and cVec
            // and then load the cSpan
            counter = 0;
            for (int i = 0; i < mr; i++)
            {
                for (int j = 0; j < nv; j++)
                {
                    // Span<double> cSpanij = MemoryMarshal.CreateSpan(ref ci, vectorSize);
                    var spanDef = il.DeclareLocal(spanType);
                    cVars[counter] = spanDef;
                    cRef = cRefs[j];
                    il.EmitLoadLocal(cRef);
                    il.EmitLoadInt(vectorSize);
                    il.EmitCall(OpCodes.Call, createSpan, null);
                    il.EmitStoreLocal(spanDef);

                    // VectorX<double> cVecij = Vector<double>.Create(cSpanij);
                    var vecDef = il.DeclareLocal(simdType);
                    cVars[counter + numVec] = vecDef;
                    il.EmitLoadLocal(spanDef);
                    il.EmitCall(OpCodes.Call, implictConv, null);
                    il.EmitCall(OpCodes.Call, createVector, null);
                    il.EmitStoreLocal(vecDef);

                    // ci = ref Unsafe.Add(ref ci, ldc);
                    il.EmitLoadLocal(cRef);
                    il.Emit(OpCodes.Ldarg_S, (byte)4);
                    il.EmitCall(OpCodes.Call, addDouble, null);
                    il.EmitStoreLocal(cRef);
                    counter++;
                }
            }

            return cVars;

        }

        private static void BuildCycleFMA(this ILGenerator il, int mr, int nv, VectorType vectorType, LocalBuilder[] cVars)
        {
            Type doubleRefType = typeof(double).MakeByRefType();
            int simdSize = vectorType switch
            {
                VectorType.Vector128 => 2,
                VectorType.Vector256 => 4,
                VectorType.Vector512 => 8,
                _ => throw new NotSupportedException("Unsupported vector type")
            };
            Type simdType = vectorType switch
            {
                VectorType.Vector128 => typeof(Vector128<double>),
                VectorType.Vector256 => typeof(Vector256<double>),
                VectorType.Vector512 => typeof(Vector512<double>),
                _ => throw new NotSupportedException("Unsupported vector type")
            };
            MethodInfo createSpan = ILUtils.CreateSpan;
            MethodInfo createVectorFromSpan = ILUtils.DynamicCreateVectorFromSpan(StaticSIMDType);
            MethodInfo createVectorFromScalar = ILUtils.DynamicCreateVectorFromScalar(StaticSIMDType);
            MethodInfo implictConv = ILUtils.ConvSpanAsReadOnly;

            // ref double aRef = ref a;
            var aRef = il.DeclareLocal(doubleRefType);
            il.Emit(OpCodes.Ldarg_1);
            il.EmitStoreLocal(aRef);
            // ref double aRef = ref a;
            var bRef = il.DeclareLocal(doubleRefType);
            il.Emit(OpCodes.Ldarg_2);
            il.EmitStoreLocal(bRef);

            //loop 
            //int p = 0
            var p = il.DeclareLocal(typeof(int));
            il.EmitLoadInt(0);
            il.EmitStoreLocal(p);
            Label loopCmp = il.DefineLabel();
            Label loopBegin = il.DefineLabel();
            il.Emit(OpCodes.Br, loopCmp);
            il.MarkLabel(loopBegin);

            LocalBuilder[] bAndAVars = new LocalBuilder[mr + nv];
            // define the local variables for a and b
            for (int j = 0; j < nv; j++)
            {
                var bVec = il.DeclareLocal(simdType);
                bAndAVars[j] = bVec;
                il.EmitLoadLocal(bRef);
                il.EmitLoadInt(simdSize);
                il.EmitCall(OpCodes.Call, createSpan, null);
                il.EmitCall(OpCodes.Call, implictConv, null);
                il.EmitCall(OpCodes.Call, createVectorFromSpan, null);
                il.EmitStoreLocal(bVec);
                il.EmitAddRef(bRef, simdSize);
            }

            // define the local variables for a
            for (int i = 0; i < mr; i++)
            {
                var aVec = il.DeclareLocal(simdType);
                bAndAVars[i + nv] = aVec;
                il.EmitLoadLocal(aRef);
                il.Emit(OpCodes.Ldind_R8);
                il.EmitCall(OpCodes.Call, createVectorFromScalar, null);
                il.EmitStoreLocal(aVec);
                il.EmitAddRef(aRef, 1);
            }

            // FMA
            int offset = mr * nv;
            for (int i = 0; i < mr; i++)
            {
                for (int j = 0; j < nv; j++)
                {
                    var cVec = cVars[i * nv + j + offset];
                    var bVec = bAndAVars[j];
                    var aVec = bAndAVars[i + nv];
                    il.EmitFMA(simdType, aVec, bVec, cVec);
                }
            }

            // p++
            il.EmitLoadLocal(p);
            il.EmitLoadInt(1);
            il.Emit(OpCodes.Add);
            il.EmitStoreLocal(p);
            il.MarkLabel(loopCmp);

            // p < k
            il.EmitLoadLocal(p);
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Blt, loopBegin);
        }

        private static void BuildStoreVectorC(this ILGenerator il, VectorType vectorType, LocalBuilder[] cVars)
        {
            int numVec = cVars.Length / 2;
            MethodInfo copyTo = ILUtils.DynamicCopyTo(StaticSIMDType);

            for (int i = 0; i < numVec; i++)
            {
                var cSpan = cVars[i];
                var cVec = cVars[i + numVec];
                il.EmitLoadLocal(cVec);
                il.EmitLoadLocal(cSpan);
                il.EmitCall(OpCodes.Call, copyTo, null);
            }
        }
        
        internal static void Setup(int mr, int nr)
        {
            DGEMM.mr = mr;
            DGEMM.nr = nr;
            BuildKernel();
        }
        #endregion
    }
}
