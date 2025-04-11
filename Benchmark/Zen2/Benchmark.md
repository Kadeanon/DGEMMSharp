```

BenchmarkDotNet v0.14.0, Windows 10 (10.0.19045.5247/22H2/2022Update)
AMD Ryzen 5 4600H with Radeon Graphics, 1 CPU, 12 logical and 6 physical cores
.NET SDK 10.0.100-preview.3.25125.5
  [Host]     : .NET 9.0.0 (9.0.24.52809), X64 RyuJIT AVX2
  DefaultJob : .NET 9.0.0 (9.0.24.52809), X64 RyuJIT AVX2


```
| Method   | Length | Mean         | Error      | StdDev     | Ratio | RatioSD | GFlops        |
|--------- |------- |-------------:|-----------:|-----------:|------:|--------:|--------------:|
| **MKL**      | **1024**   |     **50.82 ms** |   **0.829 ms** |   **0.775 ms** |  **1.00** |    **0.02** | **42.258 GFlops** |
| OpenBlas | 1024   |     50.65 ms |   1.004 ms |   1.440 ms |  1.00 |    0.03 | 42.399 GFlops |
| Auto     | 1024   |     55.63 ms |   1.079 ms |   1.403 ms |  1.09 |    0.03 | 38.603 GFlops |
|          |        |              |            |            |       |         |               |
| **MKL**      | **4096**   |  **2,993.71 ms** |  **33.092 ms** |  **29.336 ms** |  **1.00** |    **0.01** | **45.909 GFlops** |
| OpenBlas | 4096   |  3,047.97 ms |  40.012 ms |  37.427 ms |  1.02 |    0.02 | 45.092 GFlops |
| Auto     | 4096   |  3,539.21 ms |  59.641 ms |  52.870 ms |  1.18 |    0.02 | 38.833 GFlops |
|          |        |              |            |            |       |         |               |
| **MKL**      | **8192**   | **23,773.95 ms** | **164.768 ms** | **154.124 ms** |  **1.00** |    **0.01** | **46.249 GFlops** |
| OpenBlas | 8192   | 40,121.05 ms | 144.192 ms | 127.822 ms |  1.69 |    0.01 | 27.405 GFlops |
| Auto     | 8192   | 27,363.12 ms | 199.188 ms | 186.320 ms |  1.15 |    0.01 | 40.182 GFlops |
