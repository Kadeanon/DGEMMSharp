```

BenchmarkDotNet v0.14.0, Windows 10 (10.0.19045.5247/22H2/2022Update)
AMD Ryzen 5 4600H with Radeon Graphics, 1 CPU, 12 logical and 6 physical cores
.NET SDK 10.0.100-preview.3.25125.5
  [Host]     : .NET 9.0.0 (9.0.24.52809), X64 RyuJIT AVX2
  DefaultJob : .NET 9.0.0 (9.0.24.52809), X64 RyuJIT AVX2


```
| Method   | Length | Mean         | Error      | StdDev     | Ratio | RatioSD | GFlops        |
|--------- |------- |-------------:|-----------:|-----------:|------:|--------:|--------------:|
| **OpenBlas** | **1024**   |     **48.53 ms** |   **0.582 ms** |   **0.544 ms** |  **1.00** |    **0.02** | **44.246 GFlops** |
| Auto     | 1024   |     54.46 ms |   1.018 ms |   0.952 ms |  1.12 |    0.02 | 39.431 GFlops |
|          |        |              |            |            |       |         |               |
| **OpenBlas** | **4096**   |  **2,980.60 ms** |  **37.314 ms** |  **33.078 ms** |  **1.00** |    **0.02** | **46.111 GFlops** |
| Auto     | 4096   |  3,328.54 ms |  42.349 ms |  39.613 ms |  1.12 |    0.02 | 41.291 GFlops |
|          |        |              |            |            |       |         |               |
| **OpenBlas** | **8192**   | **40,154.93 ms** | **142.542 ms** | **133.333 ms** |  **1.00** |    **0.00** | **27.382 GFlops** |
| Auto     | 8192   | 26,639.26 ms | 151.953 ms | 142.137 ms |  0.66 |    0.00 | 41.274 GFlops |
