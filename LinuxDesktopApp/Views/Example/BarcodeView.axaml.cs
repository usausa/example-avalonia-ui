namespace LinuxDesktopApp.Views.Example;

using Avalonia.Controls;

using Smart.Navigation.Attributes;

using LinuxDesktopApp.Views;

[View(ViewId.Barcode)]
public partial class BarcodeView : UserControl
{
    public BarcodeView()
    {
        InitializeComponent();
    }
}
