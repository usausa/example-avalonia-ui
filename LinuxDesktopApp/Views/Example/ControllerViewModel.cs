namespace LinuxDesktopApp.Views.Example;

using System.Diagnostics;

using Avalonia.Threading;

using LinuxDesktopApp.Components.Motor;
using LinuxDesktopApp.Settings;
using LinuxDesktopApp.Views;

using LinuxDotNet.GameInput;

public sealed partial class ControllerViewModel : AppViewModelBase
{
    private static readonly (byte R, byte G, byte B)[] ShiftColors =
    [
        (0, 0, 255),       // Blue (Low speed)
        (0, 255, 255),     // Cyan
        (0, 255, 0),       // Green
        (255, 255, 0),     // Yellow
        (255, 128, 0),     // Orange
        (255, 0, 0)        // Red (High speed)
    ];

    private readonly IDispatcher dispatcher;

    private readonly GameController gamepad;

    private readonly MotorController motor;

    private readonly PeriodicTimer timer;
    private readonly CancellationTokenSource cancellationTokenSource;

    [ObservableProperty]
    public partial int Fps { get; set; }

    [ObservableProperty]
    public partial int Shift { get; set; }

    [ObservableProperty]
    public partial int Speed { get; set; }

    [ObservableProperty]
    public partial int SteeringAngle { get; set; }

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
            var fps = 0;
            var model = new GameModel();

            motor.Open();
            gamepad.Start();

            // Set initial
            var (r, g, b) = ShiftColors[model.ShiftValue];
            motor.SetLed(r, g, b);
            motor.SetServo(ServoChannel.Servo1, 90);
            motor.SetServo(ServoChannel.Servo2, 90);

            var watch = Stopwatch.StartNew();
            while (await timer.WaitForNextTickAsync(cancellationTokenSource.Token).ConfigureAwait(false))
            {
                var axis = gamepad.GetAxisValue(0);
                var accel = gamepad.GetButtonPressed(0); // A
                var brake = gamepad.GetButtonPressed(1); // B
                var shiftDown = gamepad.GetButtonPressed(2); // X
                var shiftUp = gamepad.GetButtonPressed(3); // Y

                model.Update(axis, accel, brake, shiftDown, shiftUp);

                if (model.IsUpdated)
                {
                    var shift = model.ShiftValue + 1;
                    var speed = model.Speed;
                    var steeringAngle = model.SteeringAngle;
                    var accelButton = model.Accel;
                    var brakeButton = model.Brake;
                    var shiftDownButton = model.ShiftDown;
                    var shiftUpButton = model.ShiftUp;

                    dispatcher.Post(() =>
                    {
                        Shift = shift;
                        Speed = speed;
                        SteeringAngle = steeringAngle;
                        Accel = accelButton;
                        Brake = brakeButton;
                        ShiftDown = shiftDownButton;
                        ShiftUp = shiftUpButton;
                    });

                    if (model.ShiftChanged)
                    {
                        (r, g, b) = ShiftColors[model.ShiftValue];
                        motor.SetLed(r, g, b);
                    }
                    if (model.ThrottleChanged)
                    {
                        motor.SetServo(ServoChannel.Servo2, model.ThrottleAngle);
                    }
                    if (model.SteeringChanged)
                    {
                        motor.SetServo(ServoChannel.Servo1, model.SteeringAngle);
                    }
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
            motor.SetServo(ServoChannel.Servo2, 90);
            motor.Close();
        }
    }

    private sealed class GameModel
    {
        // Shift characteristics (max speed for each gear)
        private static readonly int[] ShiftMaxSpeed = [85, 120, 160, 200, 235, 255];

        // Optimal speed range for each gear (start of power band)
        private static readonly int[] ShiftOptimalStart = [0, 40, 80, 120, 160, 200];

        // Peak torque multiplier for each gear
        private static readonly double[] ShiftTorqueMultiplier = [2.8, 2.2, 1.8, 1.5, 1.2, 1.0];

        // Speed fluctuation range at redline (±)
        private static readonly int[] ShiftSpeedFluctuation = [3, 3, 4, 4, 5, 5];

        private const double BrakeVelocity = 96d / 60;
        private const double DefaultVelocity = 32d / 60;

        private double rawSpeed;
        private readonly Random random = new();

        public int ShiftValue { get; private set; }
        public int Speed { get; private set; }

        public int ThrottleAngle { get; private set; } = 90;
        public int SteeringAngle { get; private set; } = 90;

        public bool Accel { get; private set; }
        public bool Brake { get; private set; }
        public bool ShiftDown { get; private set; }
        public bool ShiftUp { get; private set; }

        public bool IsUpdated { get; private set; }
        public bool ShiftChanged { get; private set; }
        public bool SteeringChanged { get; private set; }
        public bool ThrottleChanged { get; private set; }

        public void Update(short axis, bool accel, bool brake, bool shiftDown, bool shiftUp)
        {
            // Store previous values
            var prevShiftValue = ShiftValue;
            var prevSpeed = Speed;
            var prevThrottleAngle = ThrottleAngle;
            var prevSteeringAngle = SteeringAngle;
            var prevAccel = Accel;
            var prevBrake = Brake;
            var prevShiftDown = ShiftDown;
            var prevShiftUp = ShiftUp;

            // Update speed
            rawSpeed = CalculateSpeed(accel, brake);
            var currentSpeed = (int)rawSpeed;

            // Convert speed (0-255) to throttle angle (90-180)
            var throttleAngle = 90 + (currentSpeed * 90 / 255);
            // Convert axis (-32768 to 32767) to steering angle (0-180)
            var steeringAngle = (int)((axis + 32768) * 180.0 / 65535.0);

            // Update shift value
            if (shiftDown && !prevShiftDown && ShiftValue > 0)
            {
                ShiftValue--;
            }
            if (shiftUp && !prevShiftUp && ShiftValue < ShiftColors.Length - 1)
            {
                ShiftValue++;
            }

            // Detect changes
            ShiftChanged = ShiftValue != prevShiftValue;
            SteeringChanged = steeringAngle != prevSteeringAngle;
            ThrottleChanged = throttleAngle != prevThrottleAngle;

            IsUpdated = (currentSpeed != prevSpeed) ||
                        (steeringAngle != prevSteeringAngle) ||
                        (accel != prevAccel) ||
                        (brake != prevBrake) ||
                        (shiftDown != prevShiftDown) ||
                        (shiftUp != prevShiftUp);

            // Update properties
            Speed = currentSpeed;
            SteeringAngle = steeringAngle;
            ThrottleAngle = throttleAngle;
            Accel = accel;
            Brake = brake;
            ShiftDown = shiftDown;
            ShiftUp = shiftUp;
        }

        private double CalculateSpeed(bool accel, bool brake)
        {
            if (brake)
            {
                return Math.Max(0, rawSpeed - BrakeVelocity);
            }
            if (accel)
            {
                var velocity = CalculateAccelerationVelocity();
                var newSpeed = rawSpeed + velocity;

                // Apply redline fluctuation effect
                newSpeed = ApplyRedlineFluctuation(newSpeed);

                return Math.Min(255, newSpeed);
            }
            return Math.Max(0, rawSpeed - DefaultVelocity);
        }

        private double ApplyRedlineFluctuation(double speed)
        {
            var maxSpeedForGear = ShiftMaxSpeed[ShiftValue];
            var fluctuation = ShiftSpeedFluctuation[ShiftValue];

            // Check if we're near redline (within 95% of max speed)
            if (speed < maxSpeedForGear * 0.95)
            {
                return speed; // No fluctuation needed
            }

            // Calculate how close we are to redline (0.0 = far, 1.0 = at redline)
            var redlineProximity = Math.Min(1.0, (speed - maxSpeedForGear * 0.95) / (maxSpeedForGear * 0.05));

            // Fluctuation intensity increases as we approach redline
            var fluctuationIntensity = redlineProximity * fluctuation;

            // Add oscillation effect (simulates rev limiter bouncing)
            var oscillation = (random.NextDouble() - 0.5) * 2 * fluctuationIntensity;

            // Apply speed limit with fluctuation
            var effectiveMaxSpeed = maxSpeedForGear + fluctuation;
            var adjustedSpeed = Math.Min(effectiveMaxSpeed, speed + oscillation);

            // Prevent going below a certain threshold when hitting limiter
            var minSpeedAtLimiter = maxSpeedForGear - fluctuation;
            return Math.Max(minSpeedAtLimiter, adjustedSpeed);
        }

        private double CalculateAccelerationVelocity()
        {
            // Base acceleration rate (representing engine RPM increase)
            const double baseAcceleration = 64d / 60;

            var currentShift = ShiftValue;
            var maxSpeedForGear = ShiftMaxSpeed[currentShift];
            var optimalStart = ShiftOptimalStart[currentShift];
            var torqueMultiplier = ShiftTorqueMultiplier[currentShift];
            var fluctuation = ShiftSpeedFluctuation[currentShift];

            // Allow slight overspeed due to fluctuation, but reduce acceleration near limit
            var effectiveMaxSpeed = maxSpeedForGear + fluctuation;
            if (rawSpeed >= effectiveMaxSpeed)
            {
                return 0; // Hard limit
            }

            // Calculate the position within the gear's speed range (0.0 to 1.0)
            var gearProgress = (rawSpeed - optimalStart) / (maxSpeedForGear - optimalStart);
            gearProgress = Math.Max(0, Math.Min(1.0, gearProgress));

            // Torque curve simulation:
            // - Low gears have high torque at low speeds
            // - As speed increases within the gear, acceleration decreases
            // - Simulates engine torque curve and gearing ratio

            double torqueCurve;
            if (gearProgress < 0.3)
            {
                // Building up power (0-30% of gear range)
                torqueCurve = 0.6 + ((gearProgress / 0.3) * 0.4); // 0.6 to 1.0
            }
            else if (gearProgress < 0.7)
            {
                // Peak power range (30-70% of gear range)
                torqueCurve = 1.0;
            }
            else
            {
                // Losing power as approaching redline (70-100% of gear range)
                var fadeProgress = (gearProgress - 0.7) / 0.3;
                torqueCurve = 1.0 - (fadeProgress * 0.6); // 1.0 to 0.4
            }

            // Apply gear-specific torque multiplier
            // Lower gears have higher torque multiplication
            var effectiveAcceleration = baseAcceleration * torqueMultiplier * torqueCurve;

            // Additional penalty if below optimal speed range (lugging the engine)
            if (rawSpeed < optimalStart)
            {
                var luggingPenalty = Math.Max(0.3, rawSpeed / optimalStart);
                effectiveAcceleration *= luggingPenalty;
            }

            return effectiveAcceleration;
        }
    }
}
