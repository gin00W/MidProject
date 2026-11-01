using UnityEngine;

namespace ChessAR
{
    // A manual model preview rendered inside the interface. It does not
    // produce simulated tracked-image results or alter the AR session.
    public sealed class ChessPreviewRenderer : MonoBehaviour
    {
        const int PreviewLayer = 30;
        public RenderTexture Texture { get; private set; }
        ChessBoardVisual _visual;
        Camera _camera;
        Camera _main;
        int _mainMask;
        Mesh _paperMesh;
        Material _paperMaterial;
        Transform _root;
        float _yaw;

        public void Configure(Font font, Camera main)
        {
            _main = main;
            if (_main != null)
            {
                _mainMask = _main.cullingMask;
                _main.cullingMask = 0;
                _main.clearFlags = CameraClearFlags.SolidColor;
                _main.backgroundColor = ChessUi.Paper;
            }
            Texture = new RenderTexture(768, 768, 24) { name = "Chess Lesson Preview" };
            Texture.Create();
            _root = new GameObject("Chess Preview Scene").transform;
            _root.SetParent(transform, false);
            var cameraObject = new GameObject("Chess Preview Camera", typeof(Camera));
            cameraObject.transform.SetParent(_root, false);
            _camera = cameraObject.GetComponent<Camera>();
            _camera.targetTexture = Texture;
            _camera.cullingMask = 1 << PreviewLayer;
            _camera.clearFlags = CameraClearFlags.SolidColor;
            _camera.backgroundColor = ChessUi.Paper;
            _camera.nearClipPlane = .01f;
            _camera.farClipPlane = 10;
            _camera.fieldOfView = 40;
            var pieces = new GameObject("Preview Chess Pieces");
            pieces.transform.SetParent(_root, false);
            _visual = pieces.AddComponent<ChessBoardVisual>();
            _visual.Configure(font);
            CreatePaper();
            SetLayer(_root);
            PositionCamera();
        }

        void CreatePaper()
        {
            float x = BoardLayout.TargetWidth / 2;
            float z = BoardLayout.TargetHeight / 2;
            _paperMesh = new Mesh { name = "Printed A4 Board Preview" };
            _paperMesh.vertices = new[] { new Vector3(-x, -.0005f, -z), new Vector3(x, -.0005f, -z),
                new Vector3(x, -.0005f, z), new Vector3(-x, -.0005f, z) };
            _paperMesh.uv = new[] { Vector2.zero, Vector2.right, Vector2.one, Vector2.up };
            _paperMesh.triangles = new[] { 0, 2, 1, 0, 3, 2 };
            _paperMesh.RecalculateNormals();
            _paperMesh.RecalculateBounds();
            var paper = new GameObject("Printed Board", typeof(MeshFilter), typeof(MeshRenderer));
            paper.transform.SetParent(_root, false);
            paper.GetComponent<MeshFilter>().sharedMesh = _paperMesh;
            var shader = Resources.Load<Shader>("Chess/BoardPaper");
            if (shader == null) shader = Shader.Find("Unlit/Texture");
            _paperMaterial = new Material(shader) { name = "Printed Chess Target" };
            _paperMaterial.mainTexture = Resources.Load<Texture2D>("Chess/BoardTarget");
            paper.GetComponent<MeshRenderer>().sharedMaterial = _paperMaterial;
        }

        public void Show(ChessLessonState state)
        {
            if (_visual == null) return;
            if (state.Mode == LessonMode.Setup) _visual.ShowSetup(state.SetupStep);
            else _visual.ShowMoves(state.SelectedKind);
            SetLayer(_root);
        }

        public void Rotate()
        {
            _yaw += 30;
            PositionCamera();
        }

        void PositionCamera()
        {
            var offset = Quaternion.Euler(0, _yaw, 0) * new Vector3(0, .37f, -.33f);
            _camera.transform.position = _root.position + offset;
            _camera.transform.LookAt(_root.position);
        }

        static void SetLayer(Transform root)
        {
            root.gameObject.layer = PreviewLayer;
            foreach (Transform child in root) SetLayer(child);
        }

        void OnDestroy()
        {
            if (_main != null) _main.cullingMask = _mainMask;
            if (_camera != null) _camera.targetTexture = null;
            if (Texture != null) { Texture.Release(); Destroy(Texture); }
            if (_paperMesh != null) Destroy(_paperMesh);
            if (_paperMaterial != null) Destroy(_paperMaterial);
            if (_root != null) Destroy(_root.gameObject);
        }
    }
}
