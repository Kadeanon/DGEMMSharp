using System.Runtime.InteropServices;

namespace AutoGEMM.Benchmark
{
    internal unsafe static partial class BlasHelpers
    {

        [LibraryImport("Native/libopenblas.dll", EntryPoint = "openblas_set_num_threads")]
        public static partial void OpenBlasSetNumThreads(int n_threads);

        [LibraryImport("Native/libopenblas.dll", EntryPoint = "cblas_dgemm")]
        public static unsafe partial void OpenBlasDgemmUnsafe
            (Layout Layout, Trans TransA, Trans TransB,
            int m, int n, int k,
            double alpha,
            double* a, int lda,
            double* b, int ldb,
            double beta,
            double* c, int ldc);

        public static void OpenBlasDgemm(int m, int n, int k,
            double alpha,
            double[] arrayA, int lda,
            double[] arrayB, int ldb,
            double beta,
            double[] arrayC, int ldc)
        {
            fixed(double* a = arrayA, b = arrayB, c = arrayC)
            {
                OpenBlasDgemmUnsafe(
                    Layout.RowMajor, Trans.No, Trans.No,
                    m, n, k,
                    alpha,
                    a, lda,
                    b, ldb,
                    beta,
                    c, ldc);
            }
        }
        public static void OpenBlasDgemm(int m, int n, int k,
            double[] arrayA, int lda,
            double[] arrayB, int ldb,
            double[] arrayC, int ldc)
        {
            fixed (double* a = arrayA, b = arrayB, c = arrayC)
            {
                OpenBlasDgemmUnsafe(
                    Layout.RowMajor, Trans.No, Trans.No,
                    m, n, k,
                    1.0,
                    a, lda,
                    b, ldb,
                    0.0,
                    c, ldc);
            }
        }
    }

    internal enum Layout
    {
        RowMajor = 101,
        ColMajor = 102
    }

    internal enum Trans
    {
        No = 111,
        Yes = 112
    }
}