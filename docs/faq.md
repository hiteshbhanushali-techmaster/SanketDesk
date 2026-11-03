# Frequently Asked Questions (FAQ)

### Q: Why Windows Forms instead of WPF or modern WinUI?
**A:** Many mission-critical desktop applications (especially in finance, manufacturing, healthcare, and industrial trading) are built on Windows Forms or require lightweight background agents. Loading WPF or WinUI adds massive memory overhead, requires modern Windows App SDK deployment runtimes, and can conflict with legacy graphics loops. SanketDesk delivers modern visual aesthetics with 0% idle CPU and zero runtime dependencies.

### Q: Can I use SanketDesk in console or background worker services?
**A:** Yes, provided the host environment has an active Windows desktop session and a Windows message pump running if you wish to display visible toast forms on screen. If running headless, SanketDesk can run in history-only mode.

### Q: Does SanketDesk wake the CPU when no toasts are displayed?
**A:** No. SanketDesk uses a centralized scheduler clock that stops completely when no animations or toasts are active. CPU usage drops to **0.0%**.

### Q: How does burst coalescing work?
**A:** When `BurstStrategy.CoalesceByKey` is active and rapid duplicate alerts arrive with the same `GroupKey`, the existing active toast updates its title to `Title (N)` in-place via atomic counters rather than stacking 50 individual toast cards over your screen.

### Q: Can I customize the visual templates?
**A:** Yes. SanketDesk includes 6 built-in templates (`ModernCard`, `MinimalBar`, `SolidAccent`, `SplitPill`, `ActionPrompt`, and `WindowsFluent`) and allows registering custom template renderers implementing `INotificationTemplateRenderer`.
