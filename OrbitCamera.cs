using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Egc;

public class OrbitCamera : Camera
{
    public const float MaxElevation = 89.0f;     // grade; limitare similară cu pitch-ul la FPS

    public float MinDistance = 1.0f;
    public float MaxDistance = 50.0f;

    public float MouseSensitivity = 0.2f;        // grade pe pixel
    public float ArrowRotateSpeed = 60.0f;       // grade pe secundă
    public float ZoomStep = 0.8f;                // unitati pe pas de rotita
    public float PanSpeed = 4.0f;                // unitati pe secunda pentru WASD

    private static readonly Vector3 DefaultTarget = new(0.0f, 1.0f, 0.0f);
    private const float DefaultDistance = 8.0f;
    private const float DefaultAzimuth = 90.0f;
    private const float DefaultElevation = 20.0f;

    public Vector3 Target { get; set; } = DefaultTarget;
    public float Distance { get; set; } = DefaultDistance;
    public float Azimuth { get; set; } = DefaultAzimuth;
    public float Elevation { get; set; } = DefaultElevation;

    public OrbitCamera()
    {
        Reset();
    }

    public override string Name => "Orbită";

    private Vector3 DirectionFromTarget
    {
        get
        {
            float azRad = MathHelper.DegreesToRadians(Azimuth);
            float elRad = MathHelper.DegreesToRadians(Elevation);
            return new Vector3(
                MathF.Cos(elRad) * MathF.Cos(azRad),
                MathF.Sin(elRad),
                MathF.Cos(elRad) * MathF.Sin(azRad)
            );
        }
    }

    public override Vector3 Position => Target + Distance * DirectionFromTarget;

    public override Vector3 Front => -DirectionFromTarget;

    public override Matrix4 ViewMatrix => Matrix4.LookAt(Position, Target, Vector3.UnitY);

    public override void Update(KeyboardState keyboard, MouseState mouse, Vector2 mouseDelta, float dt)
    {
        // Rotație cu mouse-ul
        if (mouse.IsButtonDown(MouseButton.Left))
        {
            Azimuth += mouseDelta.X * MouseSensitivity;
            Elevation -= mouseDelta.Y * MouseSensitivity;
            Elevation = Math.Clamp(Elevation, -MaxElevation, MaxElevation);
        }

        // Rotație de la tastatură
        if (keyboard.IsKeyDown(Keys.Left)) Azimuth -= ArrowRotateSpeed * dt;
        if (keyboard.IsKeyDown(Keys.Right)) Azimuth += ArrowRotateSpeed * dt;
        if (keyboard.IsKeyDown(Keys.Up)) Elevation += ArrowRotateSpeed * dt;
        if (keyboard.IsKeyDown(Keys.Down)) Elevation -= ArrowRotateSpeed * dt;
        Elevation = Math.Clamp(Elevation, -MaxElevation, MaxElevation);

        //Zoom cu rotița
        if (mouse.ScrollDelta.Y != 0)
        {
            Distance = Math.Clamp(Distance - mouse.ScrollDelta.Y * ZoomStep, MinDistance, MaxDistance);
        }

        // W A S D deplasează ținta
        Vector3 forward = new Vector3(Front.X, 0.0f, Front.Z);
        if (forward.LengthSquared > 0.0001f)
        {
            forward = Vector3.Normalize(forward);
        }
        Vector3 right = Vector3.Normalize(Vector3.Cross(forward, Vector3.UnitY));

        Vector3 pan = Vector3.Zero;
        if (keyboard.IsKeyDown(Keys.W)) pan += forward;
        if (keyboard.IsKeyDown(Keys.S)) pan -= forward;
        if (keyboard.IsKeyDown(Keys.D)) pan += right;
        if (keyboard.IsKeyDown(Keys.A)) pan -= right;

        if (pan.LengthSquared > 0.0001f)
        {
            pan = Vector3.Normalize(pan);
            float speed = PanSpeed;
            if (keyboard.IsKeyDown(Keys.LeftShift))
            {
                speed *= 2.0f;
            }
            Target += pan * speed * dt;
        }
    }

    /// <summary>
    /// configurează camera orbită pornind de la starea camerei FPS
    /// </summary>
    public void SetFromFps(Vector3 fpsPosition, Vector3 fpsFront, float distance = 5.0f)
    {
        Distance = distance;
        Target = fpsPosition + distance * fpsFront;

        // Vector unitar țintă -> cameră: o = -front
        Vector3 o = -Vector3.Normalize(fpsFront);
        Elevation = MathHelper.RadiansToDegrees(MathF.Asin(Math.Clamp(o.Y, -1.0f, 1.0f)));
        Elevation = Math.Clamp(Elevation, -MaxElevation, MaxElevation);
        Azimuth = MathHelper.RadiansToDegrees(MathF.Atan2(o.Z, o.X));
    }

    public override void Reset()
    {
        Target = DefaultTarget;
        Distance = DefaultDistance;
        Azimuth = DefaultAzimuth;
        Elevation = DefaultElevation;
        FieldOfView = 60.0f;
    }

    public override IEnumerable<string> Describe()
    {
        yield return $"Țintă          : ({Target.X,6:F2}, {Target.Y,6:F2}, {Target.Z,6:F2})";
        yield return $"Distanță       : {Distance,6:F2}";
        yield return $"Azimut/Elevație: {NormalizeAngle(Azimuth),7:F1}° / {Elevation,6:F1}°";
        yield return $"Poziție        : ({Position.X,6:F2}, {Position.Y,6:F2}, {Position.Z,6:F2})";
        yield return $"Front          : ({Front.X,6:F2}, {Front.Y,6:F2}, {Front.Z,6:F2})";
    }

    private static float NormalizeAngle(float degrees)
    {
        degrees %= 360.0f;
        if (degrees > 180.0f) degrees -= 360.0f;
        if (degrees <= -180.0f) degrees += 360.0f;
        return degrees;
    }
}
