namespace LinuxDesktopApp.Markup;

using System;

using Avalonia.Markup.Xaml;
using Avalonia.Metadata;

using LinuxDesktopApp.Views;

public sealed class ViewIdExtension : MarkupExtension
{
    [Content]
    public ViewId Value { get; set; }

    public ViewIdExtension(ViewId value)
    {
        Value = value;
    }

    public override object ProvideValue(IServiceProvider serviceProvider) => Value;
}
