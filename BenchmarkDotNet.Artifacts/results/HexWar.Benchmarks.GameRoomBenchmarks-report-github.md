```

BenchmarkDotNet v0.13.12, Windows 11 (10.0.26200.8655)
13th Gen Intel Core i7-13700KF, 1 CPU, 24 logical and 16 physical cores
.NET SDK 9.0.315
  [Host]     : .NET 9.0.17 (9.0.1726.26416), X64 RyuJIT AVX2
  Job-SRXEQW : .NET 9.0.17 (9.0.1726.26416), X64 RyuJIT AVX2

Concurrent=True  Force=True  Server=True  

```
| Method                    | count | Mean          | Error       | StdDev      | Gen0     | Allocated   |
|-------------------------- |------ |--------------:|------------:|------------:|---------:|------------:|
| **CreateAndInitialize**       | **?**     |      **1.210 μs** |   **0.0239 μs** |   **0.0461 μs** |   **0.1545** |      **6.7 KB** |
| SimulateOneCompleteGame   | ?     |    174.250 μs |   3.0644 μs |   3.5289 μs |   5.3711 |   300.84 KB |
| **SimulateMultipleGamesPure** | **10**    |  **1,756.144 μs** |  **30.0824 μs** |  **28.1391 μs** |  **52.7344** |  **3007.07 KB** |
| **SimulateMultipleGamesPure** | **50**    |  **8,856.651 μs** | **153.0927 μs** | **143.2030 μs** | **265.6250** | **15054.38 KB** |
| **SimulateMultipleGamesPure** | **100**   | **17,360.275 μs** | **344.0638 μs** | **555.5988 μs** | **531.2500** | **30018.48 KB** |
