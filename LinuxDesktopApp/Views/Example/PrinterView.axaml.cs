namespace LinuxDesktopApp.Views.Example;

using Avalonia.Controls;

using Smart.Navigation.Attributes;

using LinuxDesktopApp.Views;

[View(ViewId.Printer)]
public partial class PrinterView : UserControl
{
    public PrinterView()
    {
        InitializeComponent();
    }
}
