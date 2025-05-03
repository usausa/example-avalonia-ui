namespace PosApp.Views.Main;

public sealed class MainViewModel : AppViewModelBase
{
    public string Message { get; set; }

    public MainViewModel()
    {
        Message = "Hello from MainViewModel!";
    }
}
