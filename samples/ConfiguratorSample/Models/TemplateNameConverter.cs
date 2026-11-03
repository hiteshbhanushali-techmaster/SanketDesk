using SanketDesk;
#nullable enable
using System;
using System.ComponentModel;

namespace ConfiguratorSample.Models
{
    /// <summary>
    /// TypeConverter providing dropdown choices in PropertyGrid for built-in visual template names.
    /// </summary>
    public sealed class TemplateNameConverter : StringConverter
    {
        public override bool GetStandardValuesSupported(ITypeDescriptorContext? context) => true;

        public override bool GetStandardValuesExclusive(ITypeDescriptorContext? context) => false;

        public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext? context)
        {
            return new StandardValuesCollection(new[]
            {
                NotificationTemplateNames.ModernCard,
                NotificationTemplateNames.MinimalBar,
                NotificationTemplateNames.SolidAccent,
                NotificationTemplateNames.SplitPill,
                NotificationTemplateNames.ActionPrompt,
                NotificationTemplateNames.WindowsFluent
            });
        }
    }
}
