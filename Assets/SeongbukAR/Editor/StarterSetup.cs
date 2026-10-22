using System.IO;
using Unity.XR.CoreUtils;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEditor.XR.ARSubsystems;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

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
            Directory.CreateDirectory(Root + "/Generated");
            AssetDatabase.Refresh();
            var library = CreateLibrary();
            var material = AssetDatabase.LoadAssetAtPath<Material>(Root + "/Generated/GuideGreen.mat");
            if (material == null)
            {
                material = new Material(Shader.Find("Standard")) { color = new Color(0.17f, 0.86f, 0.55f) };
                AssetDatabase.CreateAsset(material, Root + "/Generated/GuideGreen.mat");
            }
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
                var manager = originObject.AddComponent<ARTrackedImageManager>();
                manager.referenceLibrary = library;
                manager.requestedMaxNumberOfMovingImages = 1;
                var app = new GameObject("Guide App").AddComponent<GuideApp>();
                var tracker = originObject.AddComponent<CardGuideTracker>();
                tracker.app = app;
                tracker.guideMaterial = material;
                var light = new GameObject("Guide Light").AddComponent<Light>();
                light.type = LightType.Directional;
                light.transform.rotation = Quaternion.Euler(45, -30, 0);
                EditorSceneManager.SaveScene(scene, ARPath);
            }
            PlayerSettings.companyName = "StudentProject";
            PlayerSettings.productName = "Seongbuk AR Guide";
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.iOS.cameraUsageDescription = "그림카드를 인식해 AR 분리배출 안내를 표시합니다.";
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.iOS, ScriptingImplementation.IL2CPP);
            // Do not write a signing identity or replace a user's bundle identifier.
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ARPath, true) };
            AssetDatabase.SaveAssets();
            EditorSceneManager.OpenScene(PreviewPath);
            EditorUtility.DisplayDialog("시작 장면 준비 완료",
                "Play로 질문 흐름을 확인하세요.\n아이폰 실행 전 START_HERE.md의 ARKit/iOS 설정을 적용하세요.", "확인");
        }

        static XRReferenceImageLibrary CreateLibrary()
        {
            string path = Root + "/Generated/WasteCardLibrary.asset";
            var library = AssetDatabase.LoadAssetAtPath<XRReferenceImageLibrary>(path);
            if (library != null) return library;
            library = ScriptableObject.CreateInstance<XRReferenceImageLibrary>();
            AssetDatabase.CreateAsset(library, path);
            foreach (string id in new[] { "pet_bottle", "plastic_container" })
            {
                var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(Root + "/Art/" + id + ".png");
                if (texture == null) throw new FileNotFoundException("그림카드 이미지가 없습니다: " + id);
                library.Add();
                int index = library.count - 1;
                library.SetName(index, id);
                library.SetTexture(index, texture, true);
                library.SetSpecifySize(index, true);
                library.SetSize(index, new Vector2(0.12f, 0.09f));
            }
            EditorUtility.SetDirty(library);
            return library;
        }
    }
}
