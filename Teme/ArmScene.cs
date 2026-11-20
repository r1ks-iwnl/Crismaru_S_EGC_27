using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Egc;

public sealed class ArmScene : Scene
{
    private const float BaseSpeed = 70.0f;
    private const float ShoulderSpeed = 55.0f;
    private const float ElbowSpeed = 65.0f;

    private const float MinShoulder = -45.0f;
    private const float MaxShoulder = 75.0f;
    private const float MinElbow = -120.0f;
    private const float MaxElbow = 35.0f;

    private const float ClawClosedDist = 0.08f;
    private const float ClawOpenDist = 0.25f;
    private const float ClawDuration = 0.35f;

    private const string ActBaseLeft = "bază stânga";
    private const string ActBaseRight = "bază dreapta";
    private const string ActShoulderUp = "umăr sus";
    private const string ActShoulderDown = "umăr jos";
    private const string ActElbowUp = "cot sus";
    private const string ActElbowDown = "cot jos";
    private const string ActToggleClaw = "deschide / închide clește";
    private const string ActSaveKey = "salvează poză (cadru-cheie)";
    private const string ActPlayLoop = "redă pozele în buclă";
    private const string ActCycleEase = "schimbă easing redare";
    private const string ActClearKeys = "șterge pozele";
    private const string ActReset = "resetează brațul";

    private SceneObject _basePivot = null!;
    private SceneObject _shoulderPivot = null!;
    private SceneObject _elbowPivot = null!;
    private SceneObject _wristPivot = null!;

    private SceneObject _leftFinger = null!;
    private SceneObject _rightFinger = null!;
    private Tween _clawTween = null!;
    private bool _clawIsOpen;

    private float _baseAngle = 0.0f;
    private float _shoulderAngle = 20.0f;
    private float _elbowAngle = -45.0f;

    private readonly KeyframeTrack _track = new(1.25f);
    private bool _isPlaying;
    private float _playbackTime;
    private int _easeIndex;

    private SceneObject _targetCube = null!;
    private bool _isCarrying;
    private static readonly Color4 CubeFreeColor = new(0.18f, 0.78f, 0.88f, 1.0f);
    private static readonly Color4 CubeCarriedColor = new(0.25f, 0.98f, 0.45f, 1.0f);

    public override string Name => "Braț Robotic (Cobalt)";

    public override IEnumerable<string> ActionNames => new[]
    {
        ActBaseLeft, ActBaseRight,
        ActShoulderUp, ActShoulderDown,
        ActElbowUp, ActElbowDown,
        ActToggleClaw,
        ActSaveKey,
        ActPlayLoop,
        ActCycleEase,
        ActClearKeys,
        ActReset
    };

    public override string Status =>
        $"Braț: {(_isPlaying ? "REDARE BUCLĂ" : "CONTROL MANUAL")} | Bază: {_baseAngle:F0}° Umăr: {_shoulderAngle:F0}° Cot: {_elbowAngle:F0}° | Clește: {(_clawIsOpen ? "deschis" : "închis")} | Cadre: {_track.Count} (Easing: {Easing.All[_easeIndex].Name}){(_isCarrying ? " | [CONTAINER PRINS]" : "")}";

    public override void Load(Input input)
    {
        CameraPosition = new Vector3(0.0f, 4.3f, 6.4f);
        CameraTarget = new Vector3(0.0f, 1.5f, 0.0f);

        input.Bind(ActBaseLeft, Keys.A, Keys.Left);
        input.Bind(ActBaseRight, Keys.D, Keys.Right);
        input.Bind(ActShoulderUp, Keys.W, Keys.Up);
        input.Bind(ActShoulderDown, Keys.S, Keys.Down);
        input.Bind(ActElbowUp, Keys.Q, Keys.PageUp);
        input.Bind(ActElbowDown, Keys.Z, Keys.PageDown);

        input.Bind(ActToggleClaw, Keys.Space);
        input.Bind(ActSaveKey, Keys.K);
        input.Bind(ActPlayLoop, Keys.Enter, Keys.KeyPadEnter);
        input.Bind(ActCycleEase, Keys.E);
        input.Bind(ActClearKeys, Keys.Backspace);
        input.Bind(ActReset, Keys.R);

        Mesh grid = AddMesh(Shapes.Grid(10, 1.0f, new Color4(0.24f, 0.27f, 0.32f, 1.0f)));
        Mesh axes = AddMesh(Shapes.Axes(1.2f));
        Mesh cylinder = AddMesh(Shapes.ShadedCylinder(0.5f, 1.0f, 24));
        Mesh cube = AddMesh(Shapes.ShadedCube());

        AddObject("podea", grid);
        SceneObject worldAxes = AddObject("axe", axes);
        worldAxes.Transform.Position = new Vector3(-3.0f, 0.01f, -3.0f);

        Color4 chassisDark = new(0.18f, 0.20f, 0.24f, 1.0f);
        Color4 cobaltBlue = new(0.12f, 0.44f, 0.88f, 1.0f);
        Color4 lightTitanium = new(0.82f, 0.85f, 0.89f, 1.0f);
        Color4 goldAccent = new(0.96f, 0.70f, 0.16f, 1.0f);
        Color4 chromePiston = new(0.65f, 0.70f, 0.75f, 1.0f);
        Color4 gripBlack = new(0.12f, 0.13f, 0.15f, 1.0f);

        // Bază
        SceneObject basePlate = AddObject("placă bază", cube);
        basePlate.Transform.Position = new Vector3(0.0f, 0.06f, 0.0f);
        basePlate.Transform.Scale = new Vector3(1.7f, 0.12f, 1.7f);
        basePlate.Tint = chassisDark;

        foreach (float x in new[] { -0.7f, 0.7f })
        foreach (float z in new[] { -0.7f, 0.7f })
        {
            SceneObject foot = AddObject("picior bază", cylinder);
            foot.Transform.Position = new Vector3(x, 0.09f, z);
            foot.Transform.Scale = new Vector3(0.24f, 0.18f, 0.24f);
            foot.Tint = goldAccent;
        }

        SceneObject baseRiser = AddObject("guler bază", cylinder);
        baseRiser.Transform.Position = new Vector3(0.0f, 0.18f, 0.0f);
        baseRiser.Transform.Scale = new Vector3(1.35f, 0.14f, 1.35f);
        baseRiser.Tint = cobaltBlue;

        // Pivot bază (Y)
        _basePivot = AddObject("pivot bază", null);
        _basePivot.Transform.Position = new Vector3(0.0f, 0.25f, 0.0f);

        SceneObject turntable = AddObject("tambur rotativ", cylinder, _basePivot);
        turntable.Transform.Position = new Vector3(0.0f, 0.07f, 0.0f);
        turntable.Transform.Scale = new Vector3(1.05f, 0.14f, 1.05f);
        turntable.Tint = chassisDark;

        SceneObject turntableRing = AddObject("inel indicator", cylinder, _basePivot);
        turntableRing.Transform.Position = new Vector3(0.0f, 0.14f, 0.0f);
        turntableRing.Transform.Scale = new Vector3(0.90f, 0.04f, 0.90f);
        turntableRing.Tint = goldAccent;

        foreach (float z in new[] { -0.28f, 0.28f })
        {
            SceneObject fork = AddObject("furcă umăr", cube, _basePivot);
            fork.Transform.Position = new Vector3(0.0f, 0.30f, z);
            fork.Transform.Scale = new Vector3(0.35f, 0.32f, 0.12f);
            fork.Tint = cobaltBlue;
        }

        // Pivot umăr (X)
        _shoulderPivot = AddObject("pivot umăr", null, _basePivot);
        _shoulderPivot.Transform.Position = new Vector3(0.0f, 0.42f, 0.0f);

        SceneObject shoulderActuator = AddObject("actuator umăr", cylinder, _shoulderPivot);
        shoulderActuator.Transform.Rotation = new Vector3(90.0f, 0.0f, 0.0f);
        shoulderActuator.Transform.Scale = new Vector3(0.48f, 0.62f, 0.48f);
        shoulderActuator.Tint = chassisDark;

        foreach (float z in new[] { -0.32f, 0.32f })
        {
            SceneObject cap = AddObject("capac actuator", cylinder, _shoulderPivot);
            cap.Transform.Position = new Vector3(0.0f, 0.0f, z);
            cap.Transform.Rotation = new Vector3(90.0f, 0.0f, 0.0f);
            cap.Transform.Scale = new Vector3(0.40f, 0.05f, 0.40f);
            cap.Tint = goldAccent;
        }

        const float lowerArmLength = 1.8f;

        foreach (float z in new[] { -0.16f, 0.16f })
        {
            SceneObject strut = AddObject("lonjeron braț inf", cube, _shoulderPivot);
            strut.Transform.Position = new Vector3(0.0f, lowerArmLength * 0.5f, z);
            strut.Transform.Scale = new Vector3(0.22f, lowerArmLength, 0.10f);
            strut.Tint = cobaltBlue;
        }

        SceneObject centerCylinder = AddObject("piston central", cylinder, _shoulderPivot);
        centerCylinder.Transform.Position = new Vector3(0.0f, lowerArmLength * 0.5f, 0.0f);
        centerCylinder.Transform.Scale = new Vector3(0.12f, lowerArmLength * 0.90f, 0.12f);
        centerCylinder.Tint = chromePiston;

        SceneObject brace = AddObject("punte braț inf", cube, _shoulderPivot);
        brace.Transform.Position = new Vector3(0.0f, lowerArmLength * 0.5f, 0.0f);
        brace.Transform.Scale = new Vector3(0.24f, 0.15f, 0.36f);
        brace.Tint = lightTitanium;

        // Pivot cot (X)
        _elbowPivot = AddObject("pivot cot", null, _shoulderPivot);
        _elbowPivot.Transform.Position = new Vector3(0.0f, lowerArmLength, 0.0f);

        SceneObject elbowActuator = AddObject("actuator cot", cylinder, _elbowPivot);
        elbowActuator.Transform.Rotation = new Vector3(90.0f, 0.0f, 0.0f);
        elbowActuator.Transform.Scale = new Vector3(0.42f, 0.54f, 0.42f);
        elbowActuator.Tint = chassisDark;

        foreach (float z in new[] { -0.28f, 0.28f })
        {
            SceneObject cap = AddObject("capac cot", cylinder, _elbowPivot);
            cap.Transform.Position = new Vector3(0.0f, 0.0f, z);
            cap.Transform.Rotation = new Vector3(90.0f, 0.0f, 0.0f);
            cap.Transform.Scale = new Vector3(0.34f, 0.05f, 0.34f);
            cap.Tint = goldAccent;
        }

        const float upperArmLength = 1.5f;

        SceneObject upperArmBody = AddObject("corp antebraț", cube, _elbowPivot);
        upperArmBody.Transform.Position = new Vector3(0.0f, upperArmLength * 0.5f, 0.0f);
        upperArmBody.Transform.Scale = new Vector3(0.20f, upperArmLength, 0.22f);
        upperArmBody.Tint = lightTitanium;

        foreach (float x in new[] { -0.11f, 0.11f })
        {
            SceneObject plate = AddObject("panou antebraț", cube, _elbowPivot);
            plate.Transform.Position = new Vector3(x, upperArmLength * 0.5f, 0.0f);
            plate.Transform.Scale = new Vector3(0.03f, upperArmLength * 0.85f, 0.18f);
            plate.Tint = cobaltBlue;
        }

        // Pivot încheietură
        _wristPivot = AddObject("pivot încheietură", null, _elbowPivot);
        _wristPivot.Transform.Position = new Vector3(0.0f, upperArmLength, 0.0f);

        SceneObject wristCollar = AddObject("guler încheietură", cylinder, _wristPivot);
        wristCollar.Transform.Position = new Vector3(0.0f, 0.04f, 0.0f);
        wristCollar.Transform.Scale = new Vector3(0.34f, 0.08f, 0.34f);
        wristCollar.Tint = chassisDark;

        SceneObject wristBase = AddObject("bază clește", cube, _wristPivot);
        wristBase.Transform.Position = new Vector3(0.0f, 0.11f, 0.0f);
        wristBase.Transform.Scale = new Vector3(0.62f, 0.08f, 0.24f);
        wristBase.Tint = cobaltBlue;

        // Clește
        _leftFinger = AddObject("deget stânga", cube, _wristPivot);
        _leftFinger.Transform.Position = new Vector3(-ClawClosedDist, 0.27f, 0.0f);
        _leftFinger.Transform.Scale = new Vector3(0.07f, 0.28f, 0.14f);
        _leftFinger.Tint = chassisDark;

        SceneObject leftTip = AddObject("vârf stânga", cube, _leftFinger);
        leftTip.Transform.Position = new Vector3(0.35f, 0.45f, 0.0f);
        leftTip.Transform.Scale = new Vector3(0.85f, 0.25f, 0.90f);
        leftTip.Tint = goldAccent;

        SceneObject leftPad = AddObject("tampon stânga", cube, _leftFinger);
        leftPad.Transform.Position = new Vector3(0.38f, -0.05f, 0.0f);
        leftPad.Transform.Scale = new Vector3(0.30f, 0.70f, 0.75f);
        leftPad.Tint = gripBlack;

        _rightFinger = AddObject("deget dreapta", cube, _wristPivot);
        _rightFinger.Transform.Position = new Vector3(ClawClosedDist, 0.27f, 0.0f);
        _rightFinger.Transform.Scale = new Vector3(0.07f, 0.28f, 0.14f);
        _rightFinger.Tint = chassisDark;

        SceneObject rightTip = AddObject("vârf dreapta", cube, _rightFinger);
        rightTip.Transform.Position = new Vector3(-0.35f, 0.45f, 0.0f);
        rightTip.Transform.Scale = new Vector3(0.85f, 0.25f, 0.90f);
        rightTip.Tint = goldAccent;

        SceneObject rightPad = AddObject("tampon dreapta", cube, _rightFinger);
        rightPad.Transform.Position = new Vector3(-0.38f, -0.05f, 0.0f);
        rightPad.Transform.Scale = new Vector3(0.30f, 0.70f, 0.75f);
        rightPad.Tint = gripBlack;

        _clawTween = new Tween(new Vector3(ClawClosedDist, 0, 0),
                               new Vector3(ClawClosedDist, 0, 0),
                               ClawDuration, Easing.EaseInOut);
        _clawIsOpen = false;

        // Obiect bonus
        SceneObject dockPad = AddObject("platformă andocare", cylinder);
        dockPad.Transform.Position = new Vector3(1.3f, 0.12f, 0.9f);
        dockPad.Transform.Scale = new Vector3(0.85f, 0.24f, 0.85f);
        dockPad.Tint = chassisDark;

        SceneObject targetMarker = AddObject("marcă andocare", cylinder);
        targetMarker.Transform.Position = new Vector3(1.3f, 0.245f, 0.9f);
        targetMarker.Transform.Scale = new Vector3(0.65f, 0.02f, 0.65f);
        targetMarker.Tint = goldAccent;

        _targetCube = AddObject("container interactiv", cube);
        _targetCube.Transform.Position = new Vector3(1.3f, 0.40f, 0.9f);
        _targetCube.Transform.Scale = new Vector3(0.24f, 0.24f, 0.24f);
        _targetCube.Tint = CubeFreeColor;

        ApplyJointRotations();
    }

    public override void Update(float dt, Input input)
    {
        if (input.WasPressed(ActCycleEase))
        {
            _easeIndex = (_easeIndex + 1) % Easing.All.Length;
            _track.Ease = Easing.All[_easeIndex].Function;
            ConsolePanel.PrintMessage($"Easing redare: {Easing.All[_easeIndex].Name}");
        }

        if (input.WasPressed(ActSaveKey))
        {
            _track.Add(new Vector3(_baseAngle, _shoulderAngle, _elbowAngle));
            ConsolePanel.PrintMessage($"Cadru #{_track.Count} salvat: Bază={_baseAngle:F0}°, Umăr={_shoulderAngle:F0}°, Cot={_elbowAngle:F0}°");
        }

        if (input.WasPressed(ActPlayLoop))
        {
            if (_track.Count >= 2)
            {
                _isPlaying = !_isPlaying;
                if (_isPlaying) _playbackTime = 0.0f;
                ConsolePanel.PrintMessage(_isPlaying ? "Redare buclă pornită!" : "Redare oprită (control manual)");
            }
            else
            {
                ConsolePanel.PrintMessage("Salvează cel puțin 2 cadre (tasta K) pentru redare.");
            }
        }

        if (input.WasPressed(ActClearKeys))
        {
            _track.Clear();
            _isPlaying = false;
            ConsolePanel.PrintMessage("Cadrele-cheie au fost șterse.");
        }

        if (input.WasPressed(ActReset))
        {
            _isPlaying = false;
            _baseAngle = 0.0f;
            _shoulderAngle = 20.0f;
            _elbowAngle = -45.0f;
            ConsolePanel.PrintMessage("Poziție braț resetată.");
        }

        if (input.WasPressed(ActToggleClaw))
        {
            _clawIsOpen = !_clawIsOpen;
            float targetDist = _clawIsOpen ? ClawOpenDist : ClawClosedDist;
            _clawTween.GoTo(new Vector3(targetDist, 0, 0), ClawDuration);
        }

        _clawTween.Update(dt);
        float currentClawDist = _clawTween.Value.X;
        _leftFinger.Transform.Position.X = -currentClawDist;
        _rightFinger.Transform.Position.X = currentClawDist;

        if (_isPlaying && _track.Count >= 2)
        {
            _playbackTime += dt;
            Vector3 pose = _track.Sample(_playbackTime);
            _baseAngle = pose.X;
            _shoulderAngle = pose.Y;
            _elbowAngle = pose.Z;
        }
        else
        {
            float baseInput = input.Axis(ActBaseLeft, ActBaseRight);
            _baseAngle += baseInput * BaseSpeed * dt;

            float shoulderInput = input.Axis(ActShoulderDown, ActShoulderUp);
            _shoulderAngle = Math.Clamp(_shoulderAngle + shoulderInput * ShoulderSpeed * dt, MinShoulder, MaxShoulder);

            float elbowInput = input.Axis(ActElbowDown, ActElbowUp);
            _elbowAngle = Math.Clamp(_elbowAngle + elbowInput * ElbowSpeed * dt, MinElbow, MaxElbow);
        }

        ApplyJointRotations();
        UpdateObjectInteraction(currentClawDist);

        base.Update(dt, input);
    }

    private void ApplyJointRotations()
    {
        _basePivot.Transform.Rotation.Y = _baseAngle;
        _shoulderPivot.Transform.Rotation.X = _shoulderAngle;
        _elbowPivot.Transform.Rotation.X = _elbowAngle;
    }

    private void UpdateObjectInteraction(float clawDist)
    {
        var clawLocalCenter = new Vector4(0.0f, 0.28f, 0.0f, 1.0f);
        Vector3 clawWorldCenter = (clawLocalCenter * _wristPivot.WorldMatrix).Xyz;
        float distToCube = (clawWorldCenter - _targetCube.WorldPosition).Length;

        bool clawIsClosing = clawDist <= ClawClosedDist + 0.05f;

        if (!_isCarrying)
        {
            if (clawIsClosing && distToCube < 0.60f)
            {
                _isCarrying = true;
                _targetCube.Parent = _wristPivot;
                _targetCube.Transform.Position = new Vector3(0.0f, 0.28f, 0.0f);
                _targetCube.Transform.Rotation = Vector3.Zero;
                _targetCube.Tint = CubeCarriedColor;
                ConsolePanel.PrintMessage("Cleștele a apucat containerul!");
            }
        }
        else
        {
            if (!clawIsClosing)
            {
                _isCarrying = false;
                Vector3 dropPos = _targetCube.WorldPosition;
                _targetCube.Parent = null;
                _targetCube.Transform.Position = new Vector3(dropPos.X, MathF.Max(0.12f, dropPos.Y), dropPos.Z);
                _targetCube.Tint = CubeFreeColor;
                ConsolePanel.PrintMessage("Containerul a fost eliberat.");
            }
        }
    }

    public override IEnumerable<string> Describe()
    {
        foreach (string line in base.Describe()) yield return line;
        yield return $"Mod operare    : {(_isPlaying ? "Redare automată buclă" : "Manual (tastatură)")}";
        yield return $"Articulație Bază: {_baseAngle:F1}°";
        yield return $"Articulație Umăr: {_shoulderAngle:F1}° [{MinShoulder}°, {MaxShoulder}°]";
        yield return $"Articulație Cot : {_elbowAngle:F1}° [{MinElbow}°, {MaxElbow}°]";
        yield return $"Stare Clește   : {(_clawIsOpen ? "Deschis" : "Închis")} ({_clawTween.Value.X:F2})";
        yield return $"Cadre salvate  : {_track.Count} cadre ({_track.Duration:F2} s)";
        yield return $"Easing activ   : {Easing.All[_easeIndex].Name}";
        yield return $"Container bonus: {(_isCarrying ? "Prins în clește" : "Liber pe platformă/sol")}";
    }
}
