using BenchmarkDotNet.Attributes;
using System.Diagnostics;

namespace DGEMMSharp.Benchmark;

public abstract class BenchmarkBase
{
    [ParamsSource(nameof(Values))]
    public int Length { get; set; }

    public int M => Length;

    public int N => Length;

    public int K => Length;

#pragma warning disable CS8618 // 在退出构造函数时，不可为 null 的字段必须包含非 null 值。请考虑添加 "required" 修饰符或声明为可为 null。
    /// <summary>
    /// M * K
    /// </summary>
    public double[] ArrayA { get; set; }
    /// <summary>
    /// K * N
    /// </summary>
    public double[] ArrayB { get; set; }

    /// <summary>
    /// M * N
    /// </summary>
    public double[] ArrayC { get; set; }
#pragma warning restore CS8618 // 在退出构造函数时，不可为 null 的字段必须包含非 null 值。请考虑添加 "required" 修饰符或声明为可为 null。


    public IEnumerable<int> Values()
    {
        return TestValues();
    }
    public abstract IEnumerable<int> TestValues();

    [GlobalSetup]
    public void Setup()
    {
        ArrayA = new double[M * K];
        ArrayB = new double[K * N];
        ArrayC = new double[M * N];

        Random r = new();
        foreach (ref var val in ArrayA.AsSpan())
        {
            val = r.NextDouble();
        }
        foreach (ref var val in ArrayB.AsSpan())
        {
            val = r.NextDouble();
        }
        ExtraSetup();
    }

    public virtual void ExtraSetup()
    {
    }

    public void CheckDebug(Action action, double eps = 1e-8)
    {
        Setup();
        action();
        int length = Length;
        Debug.Assert(length > 0);
        double[] actual = ArrayC; 
        double[] expected = new double[length * length];
        BlasHelpers.OpenBlasDgemm(
            M, N, K, ArrayA, K, ArrayB, N, expected, N);

        bool pass = true;
        int index = 0;
        for (int i = 0; i < length; i++)
        {
            for (int j = 0; j < length; j++)
            {
                if (Math.Abs(expected[index] - actual[index]) > eps)
                {
                    Console.WriteLine($"Mismatch at ({i}, {j}): {actual[index]} != {expected[index]}");
                    pass = false;
                }
                index++;
            }
        }
        if (pass)
        {
            Console.WriteLine("All values match.");
        }
        else
        {
            Console.WriteLine("Some values do not match.");
        }
    }
}
