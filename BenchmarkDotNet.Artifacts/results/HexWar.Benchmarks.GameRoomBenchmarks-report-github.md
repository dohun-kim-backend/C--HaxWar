```

BenchmarkDotNet v0.13.12, Windows 11 (10.0.26200.8655)
13th Gen Intel Core i7-13700KF, 1 CPU, 24 logical and 16 physical cores
.NET SDK 9.0.315
  [Host]     : .NET 9.0.17 (9.0.1726.26416), X64 RyuJIT AVX2
  Job-DUFVFX : .NET 9.0.17 (9.0.1726.26416), X64 RyuJIT AVX2

Concurrent=True  Force=True  Server=True  

```
| Method              | roundCount | Mean      | Error     | StdDev    | Gen0   | Allocated |
|-------------------- |----------- |----------:|----------:|----------:|-------:|----------:|
| **CreateAndInitialize** | **?**          |  **1.207 μs** | **0.0240 μs** | **0.0420 μs** | **0.1202** |    **6.7 KB** |
| **SimulateRounds**      | **1**          |  **4.565 μs** | **0.0912 μs** | **0.1421 μs** | **0.2899** |  **16.55 KB** |
| **SimulateRounds**      | **5**          | **20.885 μs** | **0.4094 μs** | **0.6128 μs** | **0.9155** |  **51.81 KB** |
| **SimulateRounds**      | **10**         | **42.724 μs** | **0.5326 μs** | **0.4448 μs** | **1.6479** |   **95.1 KB** |
| **SimulateRounds**      | **20**         | **76.616 μs** | **1.5018 μs** | **1.7294 μs** | **3.1738** | **177.95 KB** |
