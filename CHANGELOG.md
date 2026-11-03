# Changelog

All notable changes to **SanketDesk** will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

---

## [1.0.0] - 2026-11-03

### Added
- **Initial Public Open-Source Release of SanketDesk** (formerly Notifier).
- **Pure Windows Forms Engine:** High-performance desktop notification system with zero WPF, Electron, MAUI, or web runtime dependencies.
- **Dual Runtime Support:**
  - `SanketDesk.Net8` (`net8.0-windows`) for modern .NET desktop applications.
  - `SanketDesk.NetFramework` (`net461`) for enterprise .NET Framework legacy systems.
- **Bounded Admission Controller:** Lock-free, bounded admission pipeline enforcing strict queue capacity limits at the gate.
- **Zero Per-Notification Timers:** Single shared clock coordinator pump and active toast manager that drops to 0% CPU when idle.
- **Priority System:** Strict FIFO ordering among equal priorities; deterministic priority evaluation (`Error > Warning > Info > Success`).
- **Deterministic Coalescing & Grouping:** Coalesce bursts in-place (`CoalesceByKey`, `CoalesceByTitle`) preserving occurrence count, timestamps, and IDs.
- **Dual-Limit History Retention:** Thread-safe in-memory, JSON, and SQLite storage engines enforcing both `MaxHistoryCount` and `MaxRetentionPeriod`.
- **6 Built-in GDI+ Visual Templates:** `ModernCard`, `MinimalBar`, `SolidAccent`, `SplitPill`, `ActionPrompt`, and `WindowsFluent`.
- **Interactive PropertyGrid Configurator:** Studio application (`ConfiguratorSample`) with live visual preview, PropertyGrid inspector, and C# code generator.
- **Multi-Monitor & DPI Awareness:** Dynamic DPI scaling (100% to 200%+) across 7 desktop screen anchor positions without taskbar collision.
- **Floating Action Badge:** Draggable desktop unread counter badge strictly constrained within host application form bounds.
- **Windows 10/11 Native Toasts:** WinRT native notification support with automatic fallback to Shell NotifyIcon balloons.
- **Comprehensive Benchmarks:** Automated benchmark suite (`SanketDesk.Benchmarks`) verifying 10k/100k burst ingestion throughput and zero GC collections.
