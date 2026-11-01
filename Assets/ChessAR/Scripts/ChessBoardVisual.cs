using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace ChessAR
{
    // All content is measured in metres and belongs to the registered board's
    // local X/Z plane. The printed board remains visible beneath the outlines.
    // The tracker moves this root; no billboard or independent world anchor is used.
    public sealed class ChessBoardVisual : MonoBehaviour
    {
        sealed class PieceView
        {
            public PieceKind Kind;
            public Side Side;
            public Transform Root;
            public Text Letter;
            public bool Used;
        }

        sealed class SquareView
        {
            public Transform Root;
            public Renderer Border;
            public GameObject Dot;
            public Renderer DotRenderer;
        }

        sealed class ArrowView
        {
            public Transform Root;
            public Renderer Renderer;
        }

        readonly List<PieceView> _pieces = new List<PieceView>(32);
        readonly List<ArrowView> _arrows = new List<ArrowView>(24);
        readonly List<Material> _materials = new List<Material>();
        readonly List<Mesh> _meshes = new List<Mesh>();
        readonly SquareView[] _squares = new SquareView[64];
        Material _white;
        Material _black;
        Material _whiteTrim;
        Material _blackTrim;
        Material _destination;
        Material _capture;
        Material _selected;
        Material _orientation;
        Mesh _outlineMesh;
        Mesh _dotMesh;
        Mesh _arrowMesh;
        Font _font;
        bool _built;
        int _mode = -1;
        int _stage = -1;
        PieceKind _kind;
        int _arrowCount;

        public void Configure(Font font)
        {
            _font = font != null ? font : Resources.Load<Font>("Fonts/SUIT-SemiBold");
            if (_font == null) _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            EnsureBuilt();
            foreach (var piece in _pieces)
                if (piece.Letter != null) piece.Letter.font = _font;
        }

        public void ShowSetup(int stage)
        {
            stage = Mathf.Clamp(stage, 0, 4);
            EnsureBuilt();
            if (_mode == 0 && _stage == stage) return;
            HideContent();
            _mode = 0;
            _stage = stage;
            foreach (var piece in ChessRules.InitialPosition())
            {
                bool visible = piece.Side == Side.White
                    ? stage >= (piece.Kind == PieceKind.Pawn ? 1 : 2)
                    : stage >= (piece.Kind == PieceKind.Pawn ? 3 : 4);
                if (!visible) continue;
                ShowPiece(piece);
                bool newlyAdded = stage == (piece.Side == Side.White
                    ? piece.Kind == PieceKind.Pawn ? 1 : 2
                    : piece.Kind == PieceKind.Pawn ? 3 : 4);
                if (newlyAdded) Mark(piece.Square, _destination, false);
            }
            if (stage == 0)
            {
                // h1 is light and a1 is dark: the two corners establish the
                // same orientation as the printed coordinates, not a new board.
                Mark(Square.Parse("h1"), _orientation, true);
                Mark(Square.Parse("a1"), _selected, true);
            }
            else if (stage == 2)
                Mark(Square.Parse("d1"), _orientation, false);
            else if (stage == 4)
                Mark(Square.Parse("d8"), _orientation, false);
        }

        public void ShowMoves(PieceKind kind)
        {
            EnsureBuilt();
            if (_mode == 1 && _kind == kind) return;
            var example = ChessExamples.For(kind);
            var position = example.Position;
            var destinations = example.Destinations;
            HideContent();
            _mode = 1;
            _kind = kind;
            foreach (var piece in position) ShowPiece(piece);
            foreach (var square in destinations)
            {
                bool capture = Occupant(position, square) != null;
                Mark(square, capture ? _capture : _destination, !capture);
            }
            Mark(example.SelectedPiece.Square, _selected, false);
            ShowPaths(example.SelectedPiece, position, destinations);
        }

        public void Clear()
        {
            HideContent();
            _mode = -1;
            _stage = -1;
        }

        void EnsureBuilt()
        {
            if (_built) return;
            _built = true;
            if (_font == null) _font = Resources.Load<Font>("Fonts/SUIT-SemiBold");
            if (_font == null) _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            Shader shader = Resources.Load<Shader>("Chess/LessonModel");
            if (shader == null) shader = Shader.Find("Standard");
            if (shader == null) shader = Shader.Find("Unlit/Color");
            if (shader == null) shader = Shader.Find("Hidden/InternalErrorShader");
            _white = Material(shader, "White pieces", new Color32(238, 238, 236, 255));
            _black = Material(shader, "Black pieces", new Color32(46, 46, 46, 255));
            _whiteTrim = Material(shader, "White piece detail", new Color32(156, 156, 153, 255));
            _blackTrim = Material(shader, "Black piece detail", new Color32(105, 105, 105, 255));
            _destination = Material(shader, "Movement destination", new Color32(135, 155, 166, 255));
            _capture = Material(shader, "Capture destination", new Color32(180, 110, 93, 255));
            _selected = Material(shader, "Selected source", new Color32(65, 107, 131, 255));
            _orientation = Material(shader, "Orientation and queen square", new Color32(155, 138, 108, 255));
            _outlineMesh = BuildOutline();
            _dotMesh = BuildDisk(.0028f);
            _arrowMesh = BuildArrow();
            for (int rank = 0; rank < 8; rank++)
            for (int file = 0; file < 8; file++)
            {
                var square = new Square(file, rank);
                var local = BoardLayout.ToLocal(square);
                var root = Root("Square " + square.Name, transform);
                root.localPosition = new Vector3(local.X, .0011f, local.Z);
                var border = MeshPart("Outline", root, _outlineMesh, _destination);
                var dot = MeshPart("Landing point", root, _dotMesh, _destination);
                dot.transform.localPosition = new Vector3(0, .00015f, 0);
                _squares[file + rank * 8] = new SquareView
                {
                    Root = root, Border = border, Dot = dot.gameObject, DotRenderer = dot
                };
                root.gameObject.SetActive(false);
            }
            foreach (var piece in ChessRules.InitialPosition()) CreatePiece(piece.Kind, piece.Side);
            for (int i = 0; i < 24; i++)
            {
                var root = Root("Movement path", transform);
                var renderer = MeshPart("Arrow", root, _arrowMesh, _destination);
                _arrows.Add(new ArrowView { Root = root, Renderer = renderer });
                root.gameObject.SetActive(false);
            }
        }

        void HideContent()
        {
            foreach (var piece in _pieces)
            {
                piece.Used = false;
                piece.Root.gameObject.SetActive(false);
            }
            foreach (var square in _squares)
                if (square != null) square.Root.gameObject.SetActive(false);
            foreach (var arrow in _arrows) arrow.Root.gameObject.SetActive(false);
            _arrowCount = 0;
        }

        void ShowPiece(Piece piece)
        {
            PieceView view = null;
            foreach (var candidate in _pieces)
                if (!candidate.Used && candidate.Kind == piece.Kind && candidate.Side == piece.Side)
                { view = candidate; break; }
            if (view == null) view = CreatePiece(piece.Kind, piece.Side);
            view.Used = true;
            var local = BoardLayout.ToLocal(piece.Square);
            view.Root.localPosition = new Vector3(local.X, .0018f, local.Z);
            view.Root.gameObject.SetActive(true);
        }

        void Mark(Square square, Material material, bool dot)
        {
            var view = _squares[square.File + square.Rank * 8];
            view.Root.gameObject.SetActive(true);
            view.Border.sharedMaterial = material;
            view.DotRenderer.sharedMaterial = material;
            view.Dot.SetActive(dot);
        }

        static Piece Occupant(Piece[] position, Square square)
        {
            foreach (var piece in position)
                if (piece.Square == square) return piece;
            return null;
        }

        void ShowPaths(Piece selected, Piece[] position, Square[] destinations)
        {
            if (destinations.Length == 0) return;
            if (selected.Kind == PieceKind.Knight)
            {
                // All landing squares are marked. One representative L route
                // remains readable rather than drawing eight crossing routes.
                Square destination = destinations[0];
                foreach (var square in destinations)
                    if (Occupant(position, square) != null) { destination = square; break; }
                int dx = destination.File - selected.Square.File;
                Square elbow = Math.Abs(dx) == 2
                    ? new Square(destination.File, selected.Square.Rank)
                    : new Square(selected.Square.File, destination.Rank);
                Arrow(selected.Square, elbow, _destination);
                Arrow(elbow, destination, Occupant(position, destination) != null ? _capture : _destination);
                return;
            }
            // Use the farthest allowed square in each direction. This also
            // stops a sliding path at its friendly blocker or capturable enemy.
            var endpoints = new Dictionary<int, Square>();
            foreach (var square in destinations)
            {
                int dx = square.File - selected.Square.File;
                int dz = square.Rank - selected.Square.Rank;
                int key = (Math.Sign(dx) + 1) + (Math.Sign(dz) + 1) * 3;
                if (!endpoints.TryGetValue(key, out var previous) ||
                    DistanceSquared(selected.Square, square) > DistanceSquared(selected.Square, previous))
                    endpoints[key] = square;
            }
            foreach (var square in endpoints.Values)
                Arrow(selected.Square, square, Occupant(position, square) != null ? _capture : _destination);
        }

        static int DistanceSquared(Square from, Square to)
        {
            int dx = to.File - from.File;
            int dz = to.Rank - from.Rank;
            return dx * dx + dz * dz;
        }

        void Arrow(Square from, Square to, Material material)
        {
            if (_arrowCount >= _arrows.Count) return;
            var start = BoardLayout.ToLocal(from);
            var end = BoardLayout.ToLocal(to);
            var delta = new Vector3(end.X - start.X, 0, end.Z - start.Z);
            if (delta.sqrMagnitude < .000001f) return;
            var arrow = _arrows[_arrowCount++];
            arrow.Root.localPosition = new Vector3(start.X, .00135f, start.Z);
            arrow.Root.localRotation = Quaternion.LookRotation(delta, Vector3.up);
            arrow.Root.localScale = new Vector3(.0038f, 1, delta.magnitude);
            arrow.Renderer.sharedMaterial = material;
            arrow.Root.gameObject.SetActive(true);
        }

        PieceView CreatePiece(PieceKind kind, Side side)
        {
            var root = Root(side + " " + kind, transform);
            Material body = side == Side.White ? _white : _black;
            Material trim = side == Side.White ? _whiteTrim : _blackTrim;
            Cylinder("Foot", root, new Vector3(0, .0012f, 0), .0076f, .0024f, trim);
            Cylinder("Base", root, new Vector3(0, .0029f, 0), .0067f, .0017f, body);
            Cylinder("Stem", root, new Vector3(0, .0093f, 0), .003f, .012f, body);
            Cylinder("Collar", root, new Vector3(0, .0152f, 0), .0044f, .0017f, trim);
            switch (kind)
            {
                case PieceKind.Pawn:
                    Sphere("Pawn head", root, new Vector3(0, .018f, 0), .008f, body);
                    break;
                case PieceKind.Rook:
                    Cylinder("Tower", root, new Vector3(0, .0188f, 0), .0052f, .006f, body);
                    for (int i = 0; i < 4; i++)
                    {
                        float angle = i * Mathf.PI * .5f;
                        Cube("Battlement", root,
                            new Vector3(Mathf.Sin(angle) * .0045f, .0227f, Mathf.Cos(angle) * .0045f),
                            new Vector3(.0033f, .0038f, .0033f), body);
                    }
                    break;
                case PieceKind.Knight:
                    var neck = Cube("Horse neck", root, new Vector3(0, .0195f, -.0015f),
                        new Vector3(.0055f, .012f, .007f), body);
                    neck.localRotation = Quaternion.Euler(-19, 0, 0);
                    Cube("Horse head", root, new Vector3(0, .0254f, .002f),
                        new Vector3(.006f, .005f, .0105f), body);
                    Cube("Horse muzzle", root, new Vector3(0, .0238f, .007f),
                        new Vector3(.0055f, .0036f, .005f), trim);
                    Cube("Left ear", root, new Vector3(-.0017f, .029f, -.0012f),
                        new Vector3(.0014f, .004f, .002f), body);
                    Cube("Right ear", root, new Vector3(.0017f, .029f, -.0012f),
                        new Vector3(.0014f, .004f, .002f), body);
                    break;
                case PieceKind.Bishop:
                    Primitive(PrimitiveType.Capsule, "Bishop mitre", root,
                        new Vector3(0, .022f, 0), new Vector3(.008f, .007f, .008f), body);
                    var cut = Cube("Mitre diagonal", root, new Vector3(0, .024f, -.0037f),
                        new Vector3(.0009f, .0063f, .0008f), trim);
                    cut.localRotation = Quaternion.Euler(0, 0, -25);
                    break;
                case PieceKind.Queen:
                    Cylinder("Crown band", root, new Vector3(0, .022f, 0), .0047f, .0044f, body);
                    for (int i = 0; i < 5; i++)
                    {
                        float angle = i * Mathf.PI * 2 / 5;
                        Sphere("Crown point", root,
                            new Vector3(Mathf.Sin(angle) * .0046f, .0266f, Mathf.Cos(angle) * .0046f), .0033f, body);
                    }
                    Sphere("Crown centre", root, new Vector3(0, .026f, 0), .0035f, trim);
                    break;
                case PieceKind.King:
                    Cylinder("Royal head", root, new Vector3(0, .022f, 0), .0042f, .005f, body);
                    Cube("Cross upright", root, new Vector3(0, .029f, 0),
                        new Vector3(.0022f, .009f, .0022f), body);
                    Cube("Cross arm", root, new Vector3(0, .0301f, 0),
                        new Vector3(.0072f, .0021f, .0021f), body);
                    break;
            }
            // Piece silhouettes and their base letters serve the same role as
            // the printable paper tokens. The label lies on the marker plane.
            var labelObject = new GameObject("Piece letter", typeof(RectTransform), typeof(Canvas));
            var labelRoot = labelObject.transform;
            labelRoot.SetParent(root, false);
            labelRoot.localPosition = new Vector3(0, .00385f, -.0042f);
            labelRoot.localRotation = Quaternion.Euler(90, 0, 0);
            labelRoot.localScale = Vector3.one * .00017f;
            var canvas = labelObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 1;
            var textObject = new GameObject("Letter", typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(labelRoot, false);
            var rect = textObject.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(40, 26);
            var label = textObject.GetComponent<Text>();
            label.font = _font;
            label.fontSize = 23;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = side == Side.White
                ? new Color32(45, 45, 45, 255)
                : new Color32(244, 244, 240, 255);
            label.text = Letter(kind);
            label.raycastTarget = false;
            // Keep base lettering facing the white side even for black pieces.
            // Only the horse silhouette turns towards its opponent.
            if (side == Side.Black && kind == PieceKind.Knight)
            {
                foreach (Transform child in root)
                    if (child != labelRoot)
                    {
                        child.localPosition = Quaternion.Euler(0, 180, 0) * child.localPosition;
                        child.localRotation = Quaternion.Euler(0, 180, 0) * child.localRotation;
                    }
            }
            var view = new PieceView { Kind = kind, Side = side, Root = root, Letter = label };
            _pieces.Add(view);
            root.gameObject.SetActive(false);
            return view;
        }

        static string Letter(PieceKind kind)
        {
            switch (kind)
            {
                case PieceKind.Pawn: return "P";
                case PieceKind.Rook: return "R";
                case PieceKind.Knight: return "N";
                case PieceKind.Bishop: return "B";
                case PieceKind.Queen: return "Q";
                default: return "K";
            }
        }

        Material Material(Shader shader, string name, Color color)
        {
            var material = new Material(shader) { name = name, color = color };
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            _materials.Add(material);
            return material;
        }

        static Transform Root(string name, Transform parent)
        {
            var root = new GameObject(name).transform;
            root.SetParent(parent, false);
            return root;
        }

        static Transform Cylinder(string name, Transform parent, Vector3 position,
            float radius, float height, Material material) =>
            Primitive(PrimitiveType.Cylinder, name, parent, position,
                new Vector3(radius * 2, height / 2, radius * 2), material);

        static Transform Sphere(string name, Transform parent, Vector3 position,
            float diameter, Material material) =>
            Primitive(PrimitiveType.Sphere, name, parent, position, Vector3.one * diameter, material);

        static Transform Cube(string name, Transform parent, Vector3 position,
            Vector3 size, Material material) => Primitive(PrimitiveType.Cube, name, parent, position, size, material);

        static Transform Primitive(PrimitiveType kind, string name, Transform parent,
            Vector3 position, Vector3 scale, Material material)
        {
            var part = GameObject.CreatePrimitive(kind);
            part.name = name;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = position;
            part.transform.localScale = scale;
            var renderer = part.GetComponent<Renderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            var collider = part.GetComponent<Collider>();
            if (collider != null) { collider.enabled = false; Destroy(collider); }
            return part.transform;
        }

        static Renderer MeshPart(string name, Transform parent, Mesh mesh, Material material)
        {
            var part = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            part.transform.SetParent(parent, false);
            part.GetComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = part.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return renderer;
        }

        Mesh BuildOutline()
        {
            float outer = BoardLayout.CellWidth * .465f;
            float inner = outer - .001f;
            var vertices = new List<Vector3>(16);
            var triangles = new List<int>(24);
            AddQuad(vertices, triangles, -outer, -outer, outer, -inner);
            AddQuad(vertices, triangles, -outer, inner, outer, outer);
            AddQuad(vertices, triangles, -outer, -inner, -inner, inner);
            AddQuad(vertices, triangles, inner, -inner, outer, inner);
            return Mesh("Square outline", vertices.ToArray(), triangles.ToArray());
        }

        static void AddQuad(List<Vector3> vertices, List<int> triangles,
            float left, float bottom, float right, float top)
        {
            int start = vertices.Count;
            vertices.Add(new Vector3(left, 0, bottom));
            vertices.Add(new Vector3(right, 0, bottom));
            vertices.Add(new Vector3(right, 0, top));
            vertices.Add(new Vector3(left, 0, top));
            triangles.Add(start); triangles.Add(start + 2); triangles.Add(start + 1);
            triangles.Add(start); triangles.Add(start + 3); triangles.Add(start + 2);
        }

        Mesh BuildDisk(float radius)
        {
            const int segments = 20;
            var vertices = new Vector3[segments + 1];
            var triangles = new int[segments * 3];
            for (int i = 0; i < segments; i++)
            {
                float angle = i * Mathf.PI * 2 / segments;
                vertices[i + 1] = new Vector3(Mathf.Sin(angle) * radius, 0, Mathf.Cos(angle) * radius);
                triangles[i * 3] = 0;
                triangles[i * 3 + 1] = i + 1;
                triangles[i * 3 + 2] = (i + 1) % segments + 1;
            }
            return Mesh("Destination dot", vertices, triangles);
        }

        Mesh BuildArrow()
        {
            return Mesh("Movement arrow", new[]
            {
                new Vector3(-.15f,0,0), new Vector3(.15f,0,0),
                new Vector3(.15f,0,.75f), new Vector3(-.15f,0,.75f),
                new Vector3(-.55f,0,.75f), new Vector3(0,0,1), new Vector3(.55f,0,.75f)
            }, new[] { 0,2,1, 0,3,2, 4,5,6 });
        }

        Mesh Mesh(string name, Vector3[] vertices, int[] triangles)
        {
            var mesh = new Mesh { name = name };
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            _meshes.Add(mesh);
            return mesh;
        }

        void OnDestroy()
        {
            // Piece, marker, arrow and text objects are children of this root.
            // Only meshes/materials need explicit disposal beyond that hierarchy.
            foreach (var mesh in _meshes) if (mesh != null) Destroy(mesh);
            foreach (var material in _materials) if (material != null) Destroy(material);
            _meshes.Clear();
            _materials.Clear();
        }
    }
}
