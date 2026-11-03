#nullable enable
using System;
using System.Collections.Concurrent;

namespace Notifier.Templates
{
    /// <summary>
    /// Thread-safe registry for visual template renderers.
    /// Preloaded with the 6 standard built-in template families and extensible with custom user renderers.
    /// </summary>
    public sealed class TemplateRegistry
    {
        public static readonly TemplateRegistry Instance = new TemplateRegistry();

        private readonly ConcurrentDictionary<string, INotificationTemplateRenderer> _renderers;
        private readonly INotificationTemplateRenderer _defaultRenderer;

        public TemplateRegistry()
        {
            _renderers = new ConcurrentDictionary<string, INotificationTemplateRenderer>(StringComparer.OrdinalIgnoreCase);

            var modern = new ModernCardRenderer();
            var minimal = new MinimalBarRenderer();
            var solid = new SolidAccentRenderer();
            var split = new SplitPillRenderer();
            var action = new ActionPromptRenderer();
            var fluent = new WindowsFluentRenderer();

            _renderers[modern.TemplateName] = modern;
            _renderers[minimal.TemplateName] = minimal;
            _renderers[solid.TemplateName] = solid;
            _renderers[split.TemplateName] = split;
            _renderers[action.TemplateName] = action;
            _renderers[fluent.TemplateName] = fluent;

            _defaultRenderer = modern;
        }

        /// <summary>
        /// Registers or replaces a template renderer.
        /// </summary>
        public void RegisterTemplate(INotificationTemplateRenderer renderer)
        {
            if (renderer == null) throw new ArgumentNullException(nameof(renderer));
            _renderers[renderer.TemplateName] = renderer;
        }

        /// <summary>
        /// Resolves a template renderer by name. If not found, falls back to ModernCard.
        /// </summary>
        public INotificationTemplateRenderer Resolve(string? templateName)
        {
            if (!string.IsNullOrEmpty(templateName) && _renderers.TryGetValue(templateName!, out INotificationTemplateRenderer? renderer))
            {
                return renderer;
            }
            return _defaultRenderer;
        }
    }
}
