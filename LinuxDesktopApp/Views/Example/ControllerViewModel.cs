namespace LinuxDesktopApp.Views.Example;

using System.Diagnostics;

using Avalonia.Threading;

using LinuxDesktopApp.Views;

using LinuxDotNet.GameInput;

public sealed partial class ControllerViewModel : AppViewModelBase
{
    private const double AccelVelocity1 = 64d / 60;
    private const double AccelVelocity2 = 48d / 60;
    private const double AccelVelocity3 = 32d / 60;
    private const double AccelVelocity4 = 16d / 60;
    private const double BrakeVelocity = 96d / 60;
    private const double DefaultVelocity = 32d / 60;

    private readonly IDispatcher dispatcher;

    private readonly GameController controller;

    private readonly PeriodicTimer timer;
    private readonly CancellationTokenSource cancellationTokenSource;

    [ObservableProperty]
    public partial int Fps { get; set; }

    [ObservableProperty]
    public partial int Speed { get; set; }

    [ObservableProperty]
    public partial bool Accel { get; set; }

    [ObservableProperty]
    public partial bool Brake { get; set; }

    public ControllerViewModel(
        IDispatcher dispatcher,
        GameController controller)
    {
        this.dispatcher = dispatcher;
        this.controller = controller;

        timer = new PeriodicTimer(TimeSpan.FromMilliseconds(1000d / 60));
        cancellationTokenSource = new CancellationTokenSource();

        _ = StartTimerAsync();
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
        {
            cancellationTokenSource.Cancel();
            cancellationTokenSource.Dispose();
            timer.Dispose();
        }
    }

    private async Task StartTimerAsync()
    {
        try
        {
            // Low resolution
            var fps = 0;
            var speed = 0d;
            var prevSpeed = 0;
            var prevAccel = false;
            var prevBrake = false;

            controller.Start();

            var watch = Stopwatch.StartNew();
            while (await timer.WaitForNextTickAsync(cancellationTokenSource.Token).ConfigureAwait(false))
            {
                // Speed
                var accel = controller.GetButtonPressed(0); // A
                var brake = controller.GetButtonPressed(1); // B

                if (brake)
                {
                    speed = Math.Max(0, speed - BrakeVelocity);
                }
                else if (accel)
                {
                    var velocity = speed switch
                    {
                        < 128 => AccelVelocity1,
                        < 192 => AccelVelocity2,
                        < 224 => AccelVelocity3,
                        _ => AccelVelocity4
                    };
                    speed = Math.Min(255, speed + velocity);
                }
                else
                {
                    speed = Math.Max(0, speed - DefaultVelocity);
                }

                var currentSpeed = (int)speed;
                if ((currentSpeed != prevSpeed) ||
                    (accel != prevAccel) ||
                    (brake != prevBrake))
                {
                    dispatcher.Post(() =>
                    {
                        Speed = currentSpeed;
                        Accel = accel;
                        Brake = brake;
                    });

                    prevSpeed = currentSpeed;
                    prevAccel = accel;
                    prevBrake = brake;
                }

                // FPS
                fps++;
                if (watch.ElapsedMilliseconds > 1000)
                {
                    var fpsValue = fps;
                    dispatcher.Post(() => { Fps = fpsValue; });

                    fps = 0;
                    watch.Restart();
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Ignore
        }
    }
}
