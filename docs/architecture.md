# SanketDesk Architecture Guide

This document describes the internal engineering, concurrency models, thread marshaling, and lifecycle guarantees of **SanketDesk**.

---

## 1. System Topology

SanketDesk separates concerns across four distinct operational boundaries:

```
[Host Application Producer Threads]
               │
               ▼
   [Notifier Public Facade]  (Thread-Safe, Non-Blocking)
               │
               ▼
[Bounded Admission Controller] (Lock-Free Capacity Check & Key Filter)
               │
        ┌──────┴──────┐
        ▼             ▼
[Pending Priority] [History Service]
 [Ordered Queue]   (Dual-Limit FIFO Eviction)
        │
        ▼
 [Scheduler Pump]  (Single Coordinated Animation Clock)
        │
        ▼
 [UI Dispatcher]   (Control.BeginInvoke UI Marshaling)
        │
        ▼
[Active Toast Mgr] (Multi-Monitor Coordinates & DPI Scaling)
        │
 ┌──────┼──────┐
 ▼      ▼      ▼
[Toasts][Badge][Center]
```

---

## 2. Concurrency & Hot-Path Safety

### Producer Thread Guarantee
Submitting a notification via `Notifier.Show(...)` never blocks worker threads on UI message pumps:
1. **Zero Thread Lock Enqueue:** Submissions check `Interlocked.Increment` counters against configured capacity.
2. **In-Place Burst Coalescing:** If `BurstStrategy.CoalesceByKey` is active, duplicate items update occurrence counters in-place via atomic CAS operations without triggering new UI layout recalculations.
3. **Capacity Rejection at the Gate:** If the bounded pending queue reaches `MaxPendingQueueCapacity`, submissions are rejected or dropped according to policy, preventing memory exhaustion.

### Single Coordinated Clock Architecture
Unlike legacy toast libraries that create a `System.Windows.Forms.Timer` or `Thread.Sleep` loop per active toast:
- SanketDesk utilizes a **single centralized clock timer**.
- The timer ticks at ~60 FPS (16ms intervals) only when toasts are transitioning (fading in/out or sliding).
- When toasts are statically visible or when zero toasts are on screen, the timer **stops completely**, ensuring **0% CPU consumption**.

---

## 3. UI Thread Marshaling & Placement

- All creation, drawing, animation, and destruction of `Form` instances is marshaled through `IUiDispatcher` via `Control.BeginInvoke`.
- Worker threads never directly interact with Windows GDI handles.
- Coordinate calculations dynamically query `Screen.FromControl(hostForm)` or `Screen.PrimaryScreen`, taking into account Windows Taskbar position, multi-monitor bounds, and high-DPI scaling factors (100% to 200%+).

---

## 4. Safe Teardown & Idempotence

When the host application closes or calls `Notifier.Dispose()`:
1. Ingress submissions are immediately rejected.
2. Background pump timers are stopped and disposed.
3. Active toast forms on screen are gracefully closed via non-blocking UI dispatches.
4. History buffers and SQLite connections are safely flushed and closed.
