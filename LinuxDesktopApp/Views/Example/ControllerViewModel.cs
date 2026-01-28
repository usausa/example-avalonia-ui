namespace LinuxDesktopApp.Views.Example;

using System.Diagnostics;

using Avalonia.Threading;

using LinuxDesktopApp.Components.Motor;
using LinuxDesktopApp.Settings;
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

    private static readonly (byte R, byte G, byte B)[] ShiftColors =
    [
        (255, 0, 0),       // Red (Low speed)
        (255, 128, 0),     // Orange
        (255, 255, 0),     // Yellow
        (0, 255, 0),       // Green
        (0, 255, 255),     // Cyan
        (0, 0, 255),       // Blue
        (255, 0, 255)      // Magenta (High speed)
    ];

    private readonly IDispatcher dispatcher;

    private readonly GameController gamepad;

    private readonly MotorController motor;

    private readonly PeriodicTimer timer;
    private readonly CancellationTokenSource cancellationTokenSource;

    [ObservableProperty]
    public partial int Fps { get; set; }

    [ObservableProperty]
    public partial int Speed { get; set; }

    [ObservableProperty]
    public partial int Angle { get; set; }

    [ObservableProperty]
    public partial bool Accel { get; set; }

    [ObservableProperty]
    public partial bool Brake { get; set; }

    [ObservableProperty]
    public partial bool ShiftDown { get; set; }

    [ObservableProperty]
    public partial bool ShiftUp { get; set; }

    public ControllerViewModel(
        IDispatcher dispatcher,
        MotorSetting motorSetting,
        GameController gamepad)
    {
        this.dispatcher = dispatcher;
        this.gamepad = gamepad;
        motor = new MotorController(motorSetting.Port);

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
            motor.Dispose();
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
            var prevThrottleAngle = -1;
            var prevSteeringAngle = -1;
            var shiftValue = 0;

            var prevAccel = false;
            var prevBrake = false;
            var prevShiftDown = false;
            var prevShiftUp = false;

            motor.Open();
            gamepad.Start();

            // Set initial
            var (r, g, b) = ShiftColors[shiftValue];
            motor.SetLed(r, g, b);
            motor.SetServo(ServoChannel.Servo1, 90);
            motor.SetServo(ServoChannel.Servo1, 20);

            var watch = Stopwatch.StartNew();
            while (await timer.WaitForNextTickAsync(cancellationTokenSource.Token).ConfigureAwait(false))
            {
                // Speed
                var accel = gamepad.GetButtonPressed(0); // A
                var brake = gamepad.GetButtonPressed(1); // B
                var shiftDown = gamepad.GetButtonPressed(2); // X
                var shiftUp = gamepad.GetButtonPressed(3); // Y
                var axis = gamepad.GetAxisValue(0);

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

                // Convert speed (0-255) to throttle angle (90-180)
                var throttleAngle = 90 + (currentSpeed * 90 / 255);
                // Convert axis (-32768 to 32767) to steering angle (0-180)
                var steeringAngle = (int)((axis + 32768) * 180.0 / 65535.0);

                // Shift value change
                var shiftChanged = false;
                if (shiftDown && !prevShiftDown)
                {
                    if (shiftValue > 0)
                    {
                        shiftValue--;
                        shiftChanged = true;
                    }
                }
                if (shiftUp && !prevShiftUp)
                {
                    if (shiftValue < ShiftColors.Length - 1)
                    {
                        shiftValue++;
                        shiftChanged = true;
                    }
                }

                if ((currentSpeed != prevSpeed) ||
                    (steeringAngle != prevSteeringAngle) ||
                    (accel != prevAccel) ||
                    (brake != prevBrake) ||
                    (shiftDown != prevShiftDown) ||
                    (shiftUp != prevShiftUp))
                {
                    dispatcher.Post(() =>
                    {
                        Speed = currentSpeed;
                        Accel = accel;
                        Brake = brake;
                        ShiftDown = shiftDown;
                        ShiftUp = shiftUp;
                    });

                    if (steeringAngle != prevSteeringAngle)
                    {
                        motor.SetServo(ServoChannel.Servo1, steeringAngle);
                        prevSteeringAngle = steeringAngle;
                    }

                    if (throttleAngle != prevThrottleAngle)
                    {
                        motor.SetServo(ServoChannel.Servo2, throttleAngle);
                        prevThrottleAngle = throttleAngle;
                    }

                    if (shiftChanged)
                    {
                        (r, g, b) = ShiftColors[shiftValue];
                        motor.SetLed(r, g, b);
                    }

                    prevSpeed = currentSpeed;
                    prevAccel = accel;
                    prevBrake = brake;
                    prevShiftDown = shiftDown;
                    prevShiftUp = shiftUp;
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
        finally
        {
            motor.SetLed(0, 0, 0);
            motor.SetServo(ServoChannel.Servo1, 90);
            motor.SetServo(ServoChannel.Servo1, 20);
            motor.Close();
        }
    }
}
