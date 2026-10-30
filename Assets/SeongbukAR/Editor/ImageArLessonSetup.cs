using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.XR.ARSubsystems;
using UnityEngine;
using UnityEngine.XR.ARSubsystems;

namespace SeongbukAR.Editor
{
    // Generate the real Unity asset after importing the project. Keeping this
    // in Resources also makes existing GuideAR scenes work without re-creation:
    // the runtime configures its image manager from the same resource path.
    [InitializeOnLoad]
    public static class ImageArLessonSetup
    {
        public const string ResourcePath = "Tracking/WasteLessonImages";
        public const string LibraryPath = "Assets/SeongbukAR/Resources/Tracking/WasteLessonImages.asset";
        public const float CardWidthMetres = 0.12f;

        static readonly string[] ItemIds = { "pet_bottle", "plastic_container" };
        static bool _preparing;

        static ImageArLessonSetup()
        {
            EditorApplication.delayCall += PrepareAfterImport;
        }

        static void PrepareAfterImport()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += PrepareAfterImport;
                return;
            }
            if (EditorApplication.isPlayingOrWillChangePlaymode || Application.isBatchMode) return;
            try { EnsureLibrary(); }
            catch (Exception exception)
            {
                Debug.LogError("그림 AR 학습 자료를 준비하지 못했습니다. Seongbuk AR > 4. Prepare Image AR Lesson을 실행해 주세요.\n" +
                    exception.Message);
            }
        }

        [MenuItem("Seongbuk AR/4. Prepare Image AR Lesson")]
        public static void PrepareFromMenu()
        {
            var library = EnsureLibrary();
            EditorGUIUtility.PingObject(library);
            Debug.Log("그림 AR 자료 준비 완료: 페트병·배달 용기 카드, 전체 그림 가로 12cm. 기존 GuideAR 장면을 그대로 사용할 수 있습니다.");
        }

        public static XRReferenceImageLibrary EnsureLibrary()
        {
            if (_preparing)
                throw new InvalidOperationException("그림 AR 자료를 이미 준비하고 있습니다.");
            _preparing = true;
            try
            {
                var library = AssetDatabase.LoadAssetAtPath<XRReferenceImageLibrary>(LibraryPath);
                bool changed = library == null;
                if (library == null)
                {
                    if (File.Exists(LibraryPath))
                        throw new InvalidOperationException(LibraryPath + " 파일을 참조 이미지 라이브러리로 읽을 수 없습니다.");
                    EnsureFolder("Assets/SeongbukAR/Resources/Tracking");
                    library = ScriptableObject.CreateInstance<XRReferenceImageLibrary>();
                    AssetDatabase.CreateAsset(library, LibraryPath);
                }

                foreach (string id in ItemIds)
                {
                    string imagePath = "Assets/SeongbukAR/Art/" + id + ".png";
                    var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(imagePath);
                    if (texture == null)
                    {
                        AssetDatabase.ImportAsset(imagePath, ImportAssetOptions.ForceSynchronousImport);
                        texture = AssetDatabase.LoadAssetAtPath<Texture2D>(imagePath);
                    }
                    if (texture == null || texture.width <= 0 || texture.height <= 0)
                        throw new FileNotFoundException("등록할 학습 그림을 읽을 수 없습니다: " + imagePath);
                    string textureAssetGuid = AssetDatabase.AssetPathToGUID(imagePath);
                    if (!Guid.TryParse(textureAssetGuid, out var textureGuid) || textureGuid == Guid.Empty)
                        throw new InvalidOperationException("학습 그림의 Unity GUID를 확인할 수 없습니다: " + imagePath);

                    int index = FindUniqueImage(library, id);
                    if (index < 0)
                    {
                        library.Add();
                        index = library.count - 1;
                        library.SetName(index, id);
                        changed = true;
                    }
                    var expectedSize = new Vector2(CardWidthMetres,
                        CardWidthMetres * texture.height / texture.width);
                    var reference = library[index];
                    if (reference.textureGuid != textureGuid)
                    {
                        // Keep only its asset GUID. ARKit builds native image
                        // data from the source texture; no readable CPU copy is needed.
                        library.SetTexture(index, texture, false);
                        changed = true;
                    }
                    if (!reference.specifySize || Vector2.Distance(reference.size, expectedSize) > 0.00001f)
                    {
                        library.SetSpecifySize(index, true);
                        library.SetSize(index, expectedSize);
                        changed = true;
                    }
                }

                if (changed)
                {
                    EditorUtility.SetDirty(library);
                    AssetDatabase.SaveAssets();
                }
                ValidateLibrary(library);
                return library;
            }
            finally { _preparing = false; }
        }

        static void ValidateLibrary(XRReferenceImageLibrary library)
        {
            foreach (string id in ItemIds)
            {
                int index = FindUniqueImage(library, id);
                if (index < 0 || library[index].guid == Guid.Empty ||
                    library[index].textureGuid == Guid.Empty || !library[index].specifySize ||
                    Mathf.Abs(library[index].width - CardWidthMetres) > 0.00001f)
                    throw new InvalidOperationException("그림 AR 등록 정보를 확인해 주세요: " + id);
            }
        }

        static int FindUniqueImage(XRReferenceImageLibrary library, string id)
        {
            int found = -1;
            for (int i = 0; i < library.count; i++)
            {
                if (library[i].name != id) continue;
                if (found >= 0)
                    throw new InvalidOperationException("같은 이름의 학습 그림이 중복되어 있습니다: " + id);
                found = i;
            }
            return found;
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
    }

    // Must run before AR Foundation clears native data (-1000) and ARKit
    // compiles all reference-image libraries (0). Resources libraries are
    // discovered by ARKit as well as libraries serialized into scene managers.
    public sealed class ImageArLessonBuildPreparation : IPreprocessBuildWithReport
    {
        public int callbackOrder => -2000;

        public void OnPreprocessBuild(BuildReport report)
        {
            if (report.summary.platform != BuildTarget.iOS) return;
            try { ImageArLessonSetup.EnsureLibrary(); }
            catch (Exception exception)
            {
                throw new BuildFailedException("그림 AR 학습 자료를 준비하지 못해 빌드를 중지했습니다. " + exception.Message);
            }
        }
    }
}
