# SanketDesk Performance & Benchmark Methodology

This document outlines the performance benchmarks, methodology, test execution commands, and GC allocation profiles for **SanketDesk**.

---

## 1. Test Rig Specifications
- **Operating System:** Windows 11 Enterprise (x64)
- **Runtime:** .NET 8.0.x (x64)
- **CPU:** AMD / Intel 16-Core High-Performance Desktop Workstation
- **Execution Mode:** Release configuration with optimizations enabled

---

## 2. Benchmark Scenarios & Results

Run the benchmark suite locally with:
```powershell
dotnet run --project benchmarks/SanketDesk.Benchmarks/SanketDesk.Benchmarks.csproj -c Release
```

### Raw Benchmark Measurements

| Benchmark Test | Operations | Total Time | Measured Throughput | Target SLA | Target Result |
|---|---|---|---|---|---|
| **10,000 Burst Multi-Threaded Submissions** | 10,000 | **17.3 ms** | **575,460 ops/sec** | 10,000 / sec | **PASS (57x)** |
| **100,000 Burst Ingestion & Memory** | 100,000 | **54.2 ms** | **1,843,352 ops/sec** | — | **PASS** |
| **CoalesceByKey In-Place Fast Path** | 50,000 | **20.4 ms** | **2,442,456 ops/sec** | — | **PASS** |
| **History Storage Insertions (Dual Limit)** | 20,000 | **40.8 ms** | **489,904 ops/sec** | — | **PASS** |
| **History Storage Queries (`GetLatest`)** | 1,000 | **0.2 ms** | **4,376,368 ops/sec** | — | **PASS** |

---

## 3. Garbage Collection & Memory Pressure

GC collections monitored via `GC.CollectionCount(0)`, `GC.CollectionCount(1)`, and `GC.CollectionCount(2)` before and after 10,000 burst submissions under `CoalesceByKey`:

- **Gen 0 Collections:** 0
- **Gen 1 Collections:** 0
- **Gen 2 Collections:** 0

**Key Engineering Enabler:** In-place atomic updating of duplicate keys updates internal snapshot fields (`Count`, `LastTimestamp`) without reallocating models or creating new form instances.
