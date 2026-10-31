using System;
using System.IO;
using System.Text.RegularExpressions;
using Unity.XR.CoreUtils;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEditor.XR.ARSubsystems;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace ChessAR.Editor
{
    // Source assets live in a separate folder. Only the explicit scene menu
    // changes the active build scenes and product identity; importing this
    // branch does not overwrite a user's existing scenes or signing settings.
    [InitializeOnLoad]
    public static class ChessSetup
    {
        public const string Root = "Assets/ChessAR";
        public const string PreviewScenePath = Root + "/Scenes/ChessPreview.unity";
        public const string ARScenePath = Root + "/Scenes/ChessAR.unity";
        public const string TargetPath = Root + "/Resources/Chess/BoardTarget.png";
        public const string LibraryPath = Root + "/Resources/Chess/BoardImages.asset";
        public const string LibraryResourcePath = "Chess/BoardImages";
        public const string ImageName = "chess_board";
        public const float TargetWidthMetres = 0.21f;
        public const float TargetHeightMetres = 0.297f;

        static bool _preparing;
        static bool _importPreparationQueued;

        static ChessSetup() => QueueImportPreparation();

        // Called by the source image importer as well as after domain reload.
        // A checkout can import scripts before the PNG exists, so a missing
        // target is silently deferred here. Menu/build requests fail clearly.
        internal static void QueueImportPreparation()
        {
            if (_importPreparationQueued) return;
            _importPreparationQueued = true;
            EditorApplication.delayCall += PrepareAfterImport;
        }

        static void PrepareAfterImport()
        {
            _importPreparationQueued = false;
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                QueueImportPreparation();
                return;
            }
            if (EditorApplication.isPlayingOrWillChangePlaymode || Application.isBatchMode ||
                !File.Exists(TargetPath)) return;
            try { EnsureLibrary(); }
            catch (Exception exception)
            {
                Debug.LogError("체스판 인식 자료를 준비하지 못했습니다. Chess AR > 2. Prepare Board Tracking을 실행해 주세요.\n" + exception.Message);
            }
        }

        [MenuItem("Chess AR/1. Create Chess Scenes")]
        public static void CreateScenes()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EnsureLibrary();
            EnsureFolder(Root + "/Scenes");
            if (!File.Exists(PreviewScenePath)) CreatePreviewScene();
            if (!File.Exists(ARScenePath)) CreateARScene();

            // This is an explicit choice to prepare the chess app. A separate
            // application ID allows it to coexist with the recycling app.
            PlayerSettings.productName = "Chess FirstMove";
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.iOS.cameraUsageDescription = "출력한 체스판을 인식해 말의 처음 배치와 이동법을 판 위에 표시합니다.";
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.iOS, ScriptingImplementation.IL2CPP);
            string identifier = PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.iOS);
            if (string.IsNullOrWhiteSpace(identifier) ||
                !Regex.IsMatch(identifier, @"^[A-Za-z0-9-]+(?:\.[A-Za-z0-9-]+)+$"))
                identifier = "com.studentproject.chessfirstmove";
            if (!identifier.EndsWith(".chess", StringComparison.OrdinalIgnoreCase))
                identifier += ".chess";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS, identifier);
            // Deliberately do not write appleDeveloperTeamID, automatic
            // signing, provisioning profiles or the user's camera tracking mode.
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ARScenePath, true) };
            AssetDatabase.SaveAssets();
            EditorSceneManager.OpenScene(PreviewScenePath);
            EditorUtility.DisplayDialog("체스 AR 장면 준비 완료",
                "ChessPreview에서 Play로 배치·이동법을 먼저 확인하세요.\n" +
                "기기 빌드는 ChessAR 장면을 사용합니다.\n" +
                "앱 이름: Chess FirstMove\nBundle ID: " + identifier +
                "\n기존 분리배출 장면 파일과 서명 Team은 유지됩니다.", "확인");
        }

        [MenuItem("Chess AR/2. Prepare Board Tracking")]
        public static void PrepareBoardTracking()
        {
            var library = EnsureLibrary();
            EditorGUIUtility.PingObject(library);
            Debug.Log("체스판 인식 자료 준비 완료: chess_board, A4 전체 가로 21cm × 세로 29.7cm. PDF를 실제 크기 100%로 출력하세요.");
        }

        [MenuItem("Chess AR/3. Open Chess AR Scene")]
        public static void OpenARScene() => OpenScene(ARScenePath);

        [MenuItem("Chess AR/4. Open Chess Preview")]
        public static void OpenPreviewScene() => OpenScene(PreviewScenePath);

        static void OpenScene(string path)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (!File.Exists(path))
            {
                CreateScenes();
                if (!File.Exists(path)) return;
            }
            else if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EnsureLibrary();
            EditorSceneManager.OpenScene(path);
        }

        static void CreatePreviewScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            new GameObject("Chess App").AddComponent<ChessApp>().editorPreview = true;
            if (!EditorSceneManager.SaveScene(scene, PreviewScenePath))
                throw new IOException("체스 미리보기 장면을 저장하지 못했습니다.");
        }

        static void CreateARScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var session = new GameObject("Chess AR Session");
            session.AddComponent<ARSession>();
            session.AddComponent<ARInputManager>();
            var originObject = new GameObject("Chess XR Origin");
            var origin = originObject.AddComponent<XROrigin>();
            var offset = new GameObject("Camera Offset");
            offset.transform.SetParent(originObject.transform, false);
            var cameraObject = new GameObject("Chess AR Camera");
            cameraObject.transform.SetParent(offset.transform, false);
            cameraObject.tag = "MainCamera";
            var camera = cameraObject.AddComponent<Camera>();
            camera.nearClipPlane = 0.005f;
            camera.farClipPlane = 30;
            cameraObject.AddComponent<AudioListener>();
            cameraObject.AddComponent<ARCameraManager>();
            cameraObject.AddComponent<ARCameraBackground>();
            var pose = cameraObject.AddComponent<TrackedPoseDriver>();
            var position = new InputAction("Chess AR Position", InputActionType.Value);
            position.AddBinding("<XRHMD>/centerEyePosition");
            position.AddBinding("<HandheldARInputDevice>/devicePosition");
            var rotation = new InputAction("Chess AR Rotation", InputActionType.Value);
            rotation.AddBinding("<XRHMD>/centerEyeRotation");
            rotation.AddBinding("<HandheldARInputDevice>/deviceRotation");
            pose.positionInput = new InputActionProperty(position);
            pose.rotationInput = new InputActionProperty(rotation);
            pose.ignoreTrackingState = true;
            origin.Camera = camera;
            origin.CameraFloorOffsetObject = offset;
            origin.RequestedTrackingOriginMode = XROrigin.TrackingOriginMode.Device;
            origin.CameraYOffset = 0;
            new GameObject("Chess App").AddComponent<ChessApp>();
            var light = new GameObject("Chess Light").AddComponent<Light>();
            light.type = LightType.Directional;
            light.transform.rotation = Quaternion.Euler(45, -30, 0);
            if (!EditorSceneManager.SaveScene(scene, ARScenePath))
                throw new IOException("체스 AR 장면을 저장하지 못했습니다.");
        }

        public static XRReferenceImageLibrary EnsureLibrary()
        {
            if (_preparing) throw new InvalidOperationException("체스판 인식 자료를 이미 준비하고 있습니다.");
            _preparing = true;
            try
            {
                if (!File.Exists(TargetPath))
                    throw new FileNotFoundException("체스판 원본 그림을 찾을 수 없습니다. 새 브랜치의 자료 임포트가 끝났는지 확인하세요.", TargetPath);
                var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(TargetPath);
                if (texture == null)
                {
                    AssetDatabase.ImportAsset(TargetPath, ImportAssetOptions.ForceSynchronousImport);
                    texture = AssetDatabase.LoadAssetAtPath<Texture2D>(TargetPath);
                }
                if (texture == null || texture.width <= 0 || texture.height <= 0)
                    throw new InvalidOperationException("체스판 원본 그림을 텍스처로 읽을 수 없습니다.");
                // Reject a cropped or stretched board target before creating
                // physical coordinates that disagree with the printed A4 page.
                float aspect = (float)texture.width / texture.height;
                if (Mathf.Abs(aspect - TargetWidthMetres / TargetHeightMetres) > 0.005f)
                    throw new InvalidOperationException("체스판 원본은 전체 A4 비율(21 × 29.7cm)이어야 합니다.");
                if (!Guid.TryParse(AssetDatabase.AssetPathToGUID(TargetPath), out var textureGuid) || textureGuid == Guid.Empty)
                    throw new InvalidOperationException("체스판 그림의 Unity GUID를 읽을 수 없습니다.");

                var library = AssetDatabase.LoadAssetAtPath<XRReferenceImageLibrary>(LibraryPath);
                bool changed = library == null;
                if (library == null)
                {
                    if (File.Exists(LibraryPath))
                        throw new InvalidOperationException("기존 BoardImages.asset를 참조 이미지 라이브러리로 읽을 수 없습니다.");
                    EnsureFolder(Root + "/Resources/Chess");
                    library = ScriptableObject.CreateInstance<XRReferenceImageLibrary>();
                    AssetDatabase.CreateAsset(library, LibraryPath);
                }
                if (library.count > 1)
                    throw new InvalidOperationException("체스판 라이브러리는 chess_board 이미지 하나만 사용합니다.");
                if (library.count == 0)
                {
                    library.Add();
                    changed = true;
                }
                var reference = library[0];
                if (reference.name != ImageName)
                {
                    library.SetName(0, ImageName);
                    changed = true;
                }
                if (reference.textureGuid != textureGuid)
                {
                    library.SetTexture(0, texture, false);
                    changed = true;
                }
                var expectedSize = new Vector2(TargetWidthMetres, TargetHeightMetres);
                if (!reference.specifySize || Vector2.Distance(reference.size, expectedSize) > 0.00001f)
                {
                    library.SetSpecifySize(0, true);
                    library.SetSize(0, expectedSize);
                    changed = true;
                }
                if (changed)
                {
                    EditorUtility.SetDirty(library);
                    AssetDatabase.SaveAssets();
                }
                reference = library[0];
                if (reference.guid == Guid.Empty || reference.textureGuid != textureGuid ||
                    reference.name != ImageName || !reference.specifySize ||
                    Vector2.Distance(reference.size, expectedSize) > 0.00001f)
                    throw new InvalidOperationException("체스판 이미지의 GUID·이름·실제 크기를 확인해 주세요.");
                return library;
            }
            finally { _preparing = false; }
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            if (!string.IsNullOrEmpty(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
    }

    public sealed class ChessBoardAssetImporter : AssetPostprocessor
    {
        void OnPreprocessTexture()
        {
            if (assetPath != ChessSetup.TargetPath) return;
            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Default;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.maxTextureSize = 4096;
        }

        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets,
            string[] movedAssets, string[] movedFromAssetPaths)
        {
            foreach (string path in importedAssets)
                if (path == ChessSetup.TargetPath) { ChessSetup.QueueImportPreparation(); return; }
            foreach (string path in movedAssets)
                if (path == ChessSetup.TargetPath) { ChessSetup.QueueImportPreparation(); return; }
        }
    }

    // Runs before AR Foundation clears native data (-1000) and ARKit compiles
    // reference-image libraries (0), including libraries loaded from Resources.
    public sealed class ChessBoardBuildPreparation : IPreprocessBuildWithReport
    {
        public int callbackOrder => -2100;

        public void OnPreprocessBuild(BuildReport report)
        {
            if (report.summary.platform != BuildTarget.iOS) return;
            try { ChessSetup.EnsureLibrary(); }
            catch (Exception exception)
            {
                throw new BuildFailedException("체스판 인식 자료를 준비하지 못해 빌드를 중지했습니다. " + exception.Message);
            }
        }
    }
}
