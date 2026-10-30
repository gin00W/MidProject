using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace SeongbukAR
{
    // Geometry is measured in metres and follows the tracked image's local
    // X/Z plane (+Y is above the paper). These are teaching models, not scans
    // of a particular product or a claim about its precise material.
    public sealed class WasteLessonVisual : MonoBehaviour
    {
        const int Segments = 40;
        readonly List<Material> _materials = new List<Material>();
        readonly List<Mesh> _meshes = new List<Mesh>();
        Transform _petRoot;
        Transform _bottleBody;
        Transform _cap;
        Transform _label;
        Transform _containerRoot;
        Transform _containerBody;
        Transform _lid;
        GameObject _residue;
        Material _bottleMaterial;
        Material _ridgeMaterial;
        Material _capMaterial;
        Material _labelMaterial;
        Material _stripeMaterial;
        Material _containerMaterial;
        Material _residueMaterial;
        string _currentId;
        int _step;

        public string CurrentId => _currentId;

        public void Configure(Camera camera)
        {
            // Intentionally no billboard rotation: walking around the image
            // must reveal another side of the registered three-dimensional model.
            EnsureBuilt();
        }

        public void Show(string itemId, int stepIndex)
        {
            if (itemId != "pet_bottle" && itemId != "plastic_container")
            {
                Clear();
                return;
            }
            EnsureBuilt();
            bool changedItem = _currentId != itemId;
            _currentId = itemId;
            _step = Mathf.Clamp(stepIndex, 0, 3);
            _petRoot.gameObject.SetActive(itemId == "pet_bottle");
            _containerRoot.gameObject.SetActive(itemId == "plastic_container");
            // A newly acquired card starts from a stable assembled pose. All
            // transitions afterwards interpolate, including Previous and Reset.
            if (changedItem) ApplyPose(1f, 0);
            // Residue stays visible during the inspection step. The final
            // clean example illustrates preparation; it does not inspect a
            // real container or establish that its oil can be removed.
            _residue.SetActive(itemId == "plastic_container" && _step < 3);
        }

        public void Clear()
        {
            _currentId = null;
            _step = 0;
            if (_petRoot != null) _petRoot.gameObject.SetActive(false);
            if (_containerRoot != null) _containerRoot.gameObject.SetActive(false);
        }

        void Update()
        {
            if (_currentId == null) return;
            float blend = 1f - Mathf.Exp(-8f * Time.unscaledDeltaTime);
            ApplyPose(blend, _step);
        }

        void ApplyPose(float blend, int step)
        {
            if (_currentId == "pet_bottle")
            {
                Move(_bottleBody, new Vector3(0, .005f, 0),
                    step >= 3 ? new Vector3(1.1f, .33f, 1.1f) : Vector3.one, blend);
                bool capDetached = step >= 1 && step < 3;
                // The final teaching pose closes the cap on the compressed
                // bottle again. Separation at the first step is temporary.
                Move(_cap, step >= 3 ? new Vector3(0, .0375f, 0) :
                    capDetached ? new Vector3(.037f, .112f, 0) : new Vector3(0, .094f, 0),
                    Vector3.one, blend);
                _cap.localRotation = Quaternion.Slerp(_cap.localRotation,
                    Quaternion.Euler(0, capDetached ? 145f : 0, capDetached ? -18f : 0), blend);
                Move(_label, step >= 2 ? new Vector3(-.04f, .036f, 0) : new Vector3(0, .031f, 0),
                    Vector3.one, blend);
                _label.localRotation = Quaternion.Slerp(_label.localRotation,
                    Quaternion.Euler(0, 0, step >= 2 ? 15f : 0), blend);
            }
            else if (_currentId == "plastic_container")
            {
                Move(_containerBody, step >= 3 ? new Vector3(.023f, .004f, 0) : new Vector3(0, .004f, 0),
                    Vector3.one, blend);
                Move(_lid, step >= 3 ? new Vector3(-.045f, .065f, 0) :
                    step >= 1 ? new Vector3(.025f, .067f, 0) : new Vector3(0, .038f, 0),
                    Vector3.one, blend);
                _lid.localRotation = Quaternion.Slerp(_lid.localRotation,
                    Quaternion.Euler(step >= 1 ? -18f : 0, 0, step >= 1 ? 10f : 0), blend);
            }
        }

        static void Move(Transform part, Vector3 position, Vector3 scale, float blend)
        {
            part.localPosition = Vector3.Lerp(part.localPosition, position, blend);
            part.localScale = Vector3.Lerp(part.localScale, scale, blend);
        }

        void EnsureBuilt()
        {
            if (_petRoot != null) return;
            // Select the active pipeline, rather than picking a URP shader
            // merely because its package happens to be installed.
            Shader shader = Resources.Load<Shader>("Learning/LessonModel");
            if (shader == null)
                shader = GraphicsSettings.currentRenderPipeline != null
                    ? Shader.Find("Universal Render Pipeline/Lit") : Shader.Find("Standard");
            if (shader == null)
            {
                // The primitive default material is a final fallback if an
                // optional render-pipeline shader was stripped from a player.
                var probe = GameObject.CreatePrimitive(PrimitiveType.Cube);
                probe.SetActive(false);
                shader = probe.GetComponent<Renderer>().sharedMaterial.shader;
                Destroy(probe);
            }

            _bottleMaterial = MakeMaterial(shader, "Clear PET", new Color(.76f, .88f, .91f, .55f), true);
            _ridgeMaterial = MakeMaterial(shader, "PET Edges", new Color(.62f, .77f, .8f, 1));
            _capMaterial = MakeMaterial(shader, "Bottle Cap", new Color(.88f, .9f, .91f, 1));
            _labelMaterial = MakeMaterial(shader, "Removable Label", new Color(.27f, .46f, .57f, 1));
            _stripeMaterial = MakeMaterial(shader, "Label Stripe", new Color(.94f, .95f, .95f, 1));
            _containerMaterial = MakeMaterial(shader, "Container", new Color(.84f, .89f, .88f, 1));
            _residueMaterial = MakeMaterial(shader, "Food Residue", new Color(.6f, .35f, .17f, 1));
            BuildBottle();
            BuildContainer();
            _petRoot.gameObject.SetActive(false);
            _containerRoot.gameObject.SetActive(false);
        }

        void BuildBottle()
        {
            _petRoot = PartRoot("PET Bottle Lesson", transform);
            _bottleBody = PartRoot("Compressible Bottle Body", _petRoot);
            _bottleBody.localPosition = new Vector3(0, .005f, 0);
            // Radius/height profile: foot, straight body, tapered shoulder and neck.
            var profile = new[]
            {
                new Vector2(.012f, 0), new Vector2(.016f, .004f),
                new Vector2(.0175f, .012f), new Vector2(.0175f, .053f),
                new Vector2(.016f, .062f), new Vector2(.009f, .073f),
                new Vector2(.0065f, .078f), new Vector2(.0065f, .085f)
            };
            Lathe("Bottle Shape", _bottleBody, profile, _bottleMaterial, true);
            foreach (float height in new[] { .009f, .016f, .057f })
                Cylinder("Bottle Ridge", _bottleBody, new Vector3(0, height, 0),
                    .0178f, .0011f, _ridgeMaterial);
            Cylinder("Bottle Neck Rim", _bottleBody, new Vector3(0, .082f, 0),
                .0071f, .0012f, _ridgeMaterial);

            _cap = PartRoot("Removable Cap", _petRoot);
            Cylinder("Cap", _cap, Vector3.zero, .008f, .009f, _capMaterial);
            Cylinder("Cap Rim", _cap, new Vector3(0, -.0035f, 0), .0084f, .001f, _ridgeMaterial);
            // Discrete ridges make the cap distinguishable without a texture.
            for (int i = 0; i < 16; i++)
            {
                float angle = i * Mathf.PI * 2 / 16;
                var ridge = Primitive(PrimitiveType.Cube, "Cap Grip", _cap,
                    new Vector3(Mathf.Sin(angle) * .0079f, 0, Mathf.Cos(angle) * .0079f),
                    new Vector3(.0007f, .006f, .0007f), _ridgeMaterial);
                ridge.localRotation = Quaternion.Euler(0, i * 360f / 16, 0);
            }

            _label = PartRoot("Removable Label Sleeve", _petRoot);
            Lathe("Label Wrap", _label,
                new[] { new Vector2(.018f, 0), new Vector2(.018f, .026f) }, _labelMaterial, false);
            Lathe("Label Stripe", _label,
                new[] { new Vector2(.0181f, .016f), new Vector2(.0181f, .019f) }, _stripeMaterial, false);
            _cap.localPosition = new Vector3(0, .094f, 0);
            _label.localPosition = new Vector3(0, .031f, 0);
        }

        void BuildContainer()
        {
            _containerRoot = PartRoot("Delivery Container Lesson", transform);
            _containerBody = PartRoot("Open Container Body", _containerRoot);
            _containerBody.localPosition = new Vector3(0, .004f, 0);
            Primitive(PrimitiveType.Cube, "Container Bottom", _containerBody,
                new Vector3(0, .002f, 0), new Vector3(.075f, .004f, .05f), _containerMaterial);
            Primitive(PrimitiveType.Cube, "Front Wall", _containerBody,
                new Vector3(0, .016f, -.024f), new Vector3(.075f, .03f, .002f), _containerMaterial);
            Primitive(PrimitiveType.Cube, "Back Wall", _containerBody,
                new Vector3(0, .016f, .024f), new Vector3(.075f, .03f, .002f), _containerMaterial);
            Primitive(PrimitiveType.Cube, "Left Wall", _containerBody,
                new Vector3(-.0365f, .016f, 0), new Vector3(.002f, .03f, .05f), _containerMaterial);
            Primitive(PrimitiveType.Cube, "Right Wall", _containerBody,
                new Vector3(.0365f, .016f, 0), new Vector3(.002f, .03f, .05f), _containerMaterial);

            _lid = PartRoot("Removable Container Lid", _containerRoot);
            Primitive(PrimitiveType.Cube, "Container Lid", _lid,
                Vector3.zero, new Vector3(.078f, .004f, .053f), _capMaterial);
            Primitive(PrimitiveType.Cube, "Lid Centre", _lid,
                new Vector3(0, .002f, 0), new Vector3(.064f, .001f, .039f), _containerMaterial);
            _lid.localPosition = new Vector3(0, .038f, 0);

            var residueRoot = PartRoot("Remaining Food", _containerBody);
            _residue = residueRoot.gameObject;
            Primitive(PrimitiveType.Sphere, "Residue One", residueRoot,
                new Vector3(-.014f, .008f, 0), new Vector3(.017f, .007f, .015f), _residueMaterial);
            Primitive(PrimitiveType.Sphere, "Residue Two", residueRoot,
                new Vector3(.008f, .008f, .009f), new Vector3(.021f, .006f, .012f), _residueMaterial);
            Primitive(PrimitiveType.Sphere, "Residue Three", residueRoot,
                new Vector3(.013f, .008f, -.01f), new Vector3(.014f, .008f, .012f), _residueMaterial);
        }

        static Transform PartRoot(string name, Transform parent)
        {
            var root = new GameObject(name).transform;
            root.SetParent(parent, false);
            return root;
        }

        Transform Cylinder(string name, Transform parent, Vector3 position,
            float radius, float height, Material material) =>
            Primitive(PrimitiveType.Cylinder, name, parent, position,
                new Vector3(radius * 2, height / 2, radius * 2), material);

        static Transform Primitive(PrimitiveType type, string name, Transform parent,
            Vector3 position, Vector3 scale, Material material)
        {
            var part = GameObject.CreatePrimitive(type);
            part.name = name;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = position;
            part.transform.localScale = scale;
            var renderer = part.GetComponent<Renderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            var collider = part.GetComponent<Collider>();
            if (collider != null)
            {
                collider.enabled = false;
                Destroy(collider);
            }
            return part.transform;
        }

        void Lathe(string name, Transform parent, Vector2[] profile, Material material, bool capped)
        {
            int ringVertices = Segments + 1;
            int sides = profile.Length * ringVertices;
            var vertices = new Vector3[sides + (capped ? 2 : 0)];
            var uv = new Vector2[vertices.Length];
            int triangleCount = (profile.Length - 1) * Segments * 6 + (capped ? Segments * 6 : 0);
            var triangles = new int[triangleCount];
            for (int ring = 0; ring < profile.Length; ring++)
            for (int segment = 0; segment <= Segments; segment++)
            {
                float angle = segment * Mathf.PI * 2 / Segments;
                int index = ring * ringVertices + segment;
                vertices[index] = new Vector3(Mathf.Sin(angle) * profile[ring].x,
                    profile[ring].y, Mathf.Cos(angle) * profile[ring].x);
                uv[index] = new Vector2((float)segment / Segments, (float)ring / (profile.Length - 1));
            }
            int next = 0;
            for (int ring = 0; ring < profile.Length - 1; ring++)
            for (int segment = 0; segment < Segments; segment++)
            {
                int lower = ring * ringVertices + segment;
                int upper = lower + ringVertices;
                triangles[next++] = lower;
                triangles[next++] = lower + 1;
                triangles[next++] = upper;
                triangles[next++] = lower + 1;
                triangles[next++] = upper + 1;
                triangles[next++] = upper;
            }
            if (capped)
            {
                vertices[sides] = new Vector3(0, profile[0].y, 0);
                vertices[sides + 1] = new Vector3(0, profile[profile.Length - 1].y, 0);
                int lastRing = (profile.Length - 1) * ringVertices;
                for (int segment = 0; segment < Segments; segment++)
                {
                    triangles[next++] = sides;
                    triangles[next++] = segment + 1;
                    triangles[next++] = segment;
                    triangles[next++] = sides + 1;
                    triangles[next++] = lastRing + segment;
                    triangles[next++] = lastRing + segment + 1;
                }
            }
            var mesh = new Mesh { name = name + " Mesh" };
            mesh.vertices = vertices;
            mesh.uv = uv;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            _meshes.Add(mesh);
            var part = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            part.transform.SetParent(parent, false);
            part.GetComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = part.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        Material MakeMaterial(Shader shader, string name, Color color, bool transparent = false)
        {
            var material = new Material(shader) { name = name, color = color };
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", 0);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", .28f);
            if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness", .28f);
            if (transparent)
            {
                if (material.HasProperty("_Mode")) material.SetFloat("_Mode", 3);
                if (material.HasProperty("_Surface")) material.SetFloat("_Surface", 1);
                if (material.HasProperty("_SrcBlend")) material.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
                if (material.HasProperty("_DstBlend")) material.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
                if (material.HasProperty("_ZWrite")) material.SetInt("_ZWrite", 0);
                material.SetOverrideTag("RenderType", "Transparent");
                material.DisableKeyword("_ALPHATEST_ON");
                material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                material.EnableKeyword("_ALPHABLEND_ON");
                material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                material.renderQueue = (int)RenderQueue.Transparent;
            }
            _materials.Add(material);
            return material;
        }

        void OnDestroy()
        {
            if (_petRoot != null) Destroy(_petRoot.gameObject);
            if (_containerRoot != null) Destroy(_containerRoot.gameObject);
            foreach (var mesh in _meshes)
                if (mesh != null) Destroy(mesh);
            foreach (var material in _materials)
                if (material != null) Destroy(material);
            _meshes.Clear();
            _materials.Clear();
        }
    }
}
