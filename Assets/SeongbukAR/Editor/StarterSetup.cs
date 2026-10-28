using System.IO;
using System.Linq;
using Unity.XR.CoreUtils;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using UnityEngine.XR.ARFoundation;

namespace SeongbukAR.Editor
{
    public static class StarterSetup
    {
        const string Root = "Assets/SeongbukAR";
        const string PreviewPath = Root + "/Scenes/GuidePreview.unity";
        const string ARPath = Root + "/Scenes/GuideAR.unity";

        [MenuItem("Seongbuk AR/1. Create Starter Scenes")]
        public static void CreateScenes()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Directory.CreateDirectory(Root + "/Scenes");
            AssetDatabase.Refresh();
            if (!File.Exists(PreviewPath))
            {
                var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
                new GameObject("Guide App").AddComponent<GuideApp>().editorPreview = true;
                EditorSceneManager.SaveScene(scene, PreviewPath);
            }
            if (!File.Exists(ARPath))
            {
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                var session = new GameObject("AR Session");
                session.AddComponent<ARSession>();
                session.AddComponent<ARInputManager>();
                var originObject = new GameObject("XR Origin");
                var origin = originObject.AddComponent<XROrigin>();
                var offset = new GameObject("Camera Offset");
                offset.transform.SetParent(originObject.transform, false);
                var cameraObject = new GameObject("AR Camera");
                cameraObject.transform.SetParent(offset.transform, false);
                cameraObject.tag = "MainCamera";
                var camera = cameraObject.AddComponent<Camera>();
                camera.nearClipPlane = 0.01f;
                camera.farClipPlane = 30;
                cameraObject.AddComponent<AudioListener>();
                cameraObject.AddComponent<ARCameraManager>();
                cameraObject.AddComponent<ARCameraBackground>();
                var poseDriver = cameraObject.AddComponent<TrackedPoseDriver>();
                var position = new InputAction("AR Camera Position", InputActionType.Value);
                position.AddBinding("<XRHMD>/centerEyePosition");
                position.AddBinding("<HandheldARInputDevice>/devicePosition");
                var rotation = new InputAction("AR Camera Rotation", InputActionType.Value);
                rotation.AddBinding("<XRHMD>/centerEyeRotation");
                rotation.AddBinding("<HandheldARInputDevice>/deviceRotation");
                poseDriver.positionInput = new InputActionProperty(position);
                poseDriver.rotationInput = new InputActionProperty(rotation);
                poseDriver.ignoreTrackingState = true;
                origin.Camera = camera;
                origin.CameraFloorOffsetObject = offset;
                origin.RequestedTrackingOriginMode = XROrigin.TrackingOriginMode.Device;
                origin.CameraYOffset = 0;
                new GameObject("Guide App").AddComponent<GuideApp>();
                var light = new GameObject("Guide Light").AddComponent<Light>();
                light.type = LightType.Directional;
                light.transform.rotation = Quaternion.Euler(45, -30, 0);
                EditorSceneManager.SaveScene(scene, ARPath);
            }
            PlayerSettings.companyName = "StudentProject";
            PlayerSettings.productName = "Seongbuk AR Guide";
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.iOS.cameraUsageDescription = "물건을 스캔해 분리배출 방법과 공간 안내를 표시합니다.";
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.iOS, ScriptingImplementation.IL2CPP);
            // Do not write a signing identity or replace a user's bundle identifier.
            var buildScenes = EditorBuildSettings.scenes.ToList();
            if (!buildScenes.Any(scene => scene.path == ARPath))
                buildScenes.Insert(0, new EditorBuildSettingsScene(ARPath, true));
            EditorBuildSettings.scenes = buildScenes.ToArray();
            AssetDatabase.SaveAssets();
            EditorSceneManager.OpenScene(PreviewPath);
            EditorUtility.DisplayDialog("시작 장면 준비 완료",
                "Play로 새 화면과 품목 검색을 확인하세요.\n실물 스캔과 공간 안내는 아이폰에 빌드해 확인합니다.\nSTART_HERE.md의 ARKit/iOS 설정을 먼저 적용하세요.", "확인");
        }

        [MenuItem("Seongbuk AR/2. Check Catalog")]
        public static void CheckCatalog()
        {
            var text = Resources.Load<TextAsset>("Data/seongbuk-demo");
            if (text == null) throw new FileNotFoundException("품목 목록을 찾을 수 없습니다.");
            var catalog = new WasteCatalog(JsonUtility.FromJson<WasteCatalogData>(text.text));
            EditorUtility.DisplayDialog("품목 목록 확인", catalog.Data.items.Length + "개 품목의 질문 연결을 확인했습니다.", "확인");
        }

        [MenuItem("Seongbuk AR/3. Open iPhone Scan Scene")]
        public static void OpenScanScene()
        {
            if (!File.Exists(ARPath)) CreateScenes();
            if (!File.Exists(ARPath) || !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.OpenScene(ARPath);
        }
    }
}
