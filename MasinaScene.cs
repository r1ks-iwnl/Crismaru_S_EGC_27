using OpenTK.Mathematics;
namespace Egc
{
    public sealed class MasinaScene : Scene
    {
        private const string Restart = "repornește mașina";
        private SceneObject car = null!;
        private Tween carTween = null!;
        private SceneObject orbit = null!;

        public override string Name => "test";
        public override IEnumerable<string> ActionNames => new[] { Restart };

        public override void Load(Input input)
        {
            CameraPosition = new Vector3(0.0f, 6.0f, 10.0f);
            CameraTarget = new Vector3(0.0f, 0.8f, 0.0f);
            input.Bind(Restart, OpenTK.Windowing.GraphicsLibraryFramework.Keys.R);

            Mesh cube = AddMesh(Shapes.ShadedCube());
            Mesh sphere = AddMesh(Shapes.ShadedSphere(1.0f, 24, 12));
            Mesh wheel = AddMesh(Shapes.ShadedCylinder(0.32f, 0.24f, 24));
            Mesh floor = AddMesh(Shapes.Grid(10, 1.0f, new Color4(0.30f, 0.33f, 0.38f, 1.0f)));

            AddObject("podea", floor);
            BuildCar(cube, sphere, wheel);
            BuildPath(sphere);
        }

        public void BuildPath(Mesh sphere)
        {
            List<Vector3> _path = new();
            _path.Add(new Vector3(-5.0f, 0.65f, 0.0f));
            _path.Add(new Vector3(5.0f, 0.65f, 0.0f));

            // Un marcaj mic în locul fiecărei chei.
            for (int i = 0; i < _path.Count; i++)
            {
                SceneObject marker = AddObject($"cheia {i}", sphere);
                marker.Transform.Position = _path[i];
                marker.Transform.Scale = new Vector3(0.15f);
                marker.Tint = new Color4(1.0f, 0.9f, 0.3f, 1.0f);
            }
        }

        private void BuildCar(Mesh cube, Mesh sphere, Mesh wheel)
        {
            car = AddObject("mașină", null);
            carTween = new Tween(new Vector3(-5.0f, 0.65f, 0.0f),
                                 new Vector3(5.0f, 0.65f, 0.0f),
                                 4.0f, Easing.EaseInOut, pingPong: true);
            car.Transform.Position = carTween.From;

            SceneObject body = AddObject("caroserie", cube, car);
            body.Transform.Scale = new Vector3(2.2f, 0.55f, 1.1f);
            body.Tint = new Color4(0.1f, 0.35f, 0.85f, 1.0f);

            SceneObject cabin = AddObject("cabină", cube, car);
            cabin.Transform.Position = new Vector3(-0.15f, 0.62f, 0.0f);
            cabin.Transform.Scale = new Vector3(1.15f, 0.55f, 0.9f);
            cabin.Tint = new Color4(0.2f, 0.75f, 0.9f, 1.0f);

            foreach (float x in new[] { -0.85f, 0.85f })
            foreach (float z in new[] { -0.6f, 0.6f })
            {
                SceneObject tire = AddObject("roată", wheel, car);
                tire.Transform.Position = new Vector3(x, -0.42f, z);
                tire.Transform.Rotation = new Vector3(90.0f, 0.0f, 0.0f);
                tire.Tint = new Color4(0.04f, 0.04f, 0.05f, 1.0f);
            }

            orbit = AddObject("orbita", null, car);
            orbit.Transform.Position = new Vector3(0.0f, 1.0f, 0.0f);
            orbit.Spin = new Vector3(0.0f, 140.0f, 0.0f);

            SceneObject ball = AddObject("sferă orbitantă", sphere, orbit);
            ball.Transform.Position = new Vector3(2.0f, 0.0f, 0.0f);
            ball.Transform.Scale = new Vector3(0.3f);
            ball.Tint = new Color4(1.0f, 0.3f, 0.2f, 1.0f);
        }

        public override void Update(float dt, Input input)
        {
            base.Update(dt, input);

            if (input.WasPressed(Restart))
            {
                carTween.Restart();
                car.Transform.Position = carTween.From;
                orbit.Transform.Rotation = Vector3.Zero;
            }

            carTween.Update(dt);
            car.Transform.Position = carTween.Value;
        }

        public override IEnumerable<string> Describe()
        {
            yield return $"Mașină: x = {car.Transform.Position.X:F2}, deplasare între -5 și 5";
            yield return "Sfera orbitează în jurul mașinii; R repornește animația.";
        }
    }
}
