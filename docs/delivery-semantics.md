# SanketDesk Delivery Semantics & Operational Guarantees

This document defines the exact delivery contracts, threading boundaries, and operational limits of **SanketDesk**.

---

## 1. What "Delivery" Means in SanketDesk

Calling `Notifier.Show(...)` returns a non-zero notification identifier:

> **IMPORTANT**: A successful return from `Notifier.Show(...)` indicates **successful admission into the engine's ingress queue**.

It **does not** mean:
- The notification has been physically drawn on the screen yet.
- The end user has read, seen, or interacted with the notification.
- The notification is guaranteed to display if the host process terminates before the animation queue drains.

---

## 2. Concurrency & Failure Isolation

- **Non-Throwing Public API:** Calling `Notifier.Show(...)` will not throw unhandled exceptions into host application threads. If the engine is disposed, uninitialized, or queue capacity is exceeded, it returns `0` or an error code.
- **Worker Thread Protection:** Worker threads calling the facade never block waiting for Windows Forms message loops (`Application.DoEvents` or synchronous `Control.Invoke`). Submissions dispatch asynchronously via non-blocking queues.
- **UI Thread Exclusivity:** All GDI+ drawing, window sizing, and coordinate placement executes strictly on the UI thread via `Control.BeginInvoke`.

---

## 3. Operational Boundaries & Volatility

- **In-Memory Buffering:** By default, history entries and pending queues reside in volatile process memory. If the host application process crashes or is forcefully terminated, unpersisted history entries are lost. Use SQLite storage configuration for persistent cross-session history.
- **Display Slots:** When `MaxVisibleNotifications` is reached (e.g. 3), subsequent alerts remain in the priority queue until an active toast expires or is dismissed.
- **Platform Limitation:** SanketDesk utilizes Windows Forms and GDI+ and requires Windows operating systems.
