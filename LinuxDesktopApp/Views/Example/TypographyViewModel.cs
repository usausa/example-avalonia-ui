namespace LinuxDesktopApp.Views.Example;

using LinuxDesktopApp.Views;

public sealed partial class TypographyViewModel : AppViewModelBase
{
    [ObservableProperty]
    public partial string Message { get; set; }

    public TypographyViewModel()
    {
        Message = "Hello from TypographyViewModel!";
    }
}
