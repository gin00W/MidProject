#if UNITY_EDITOR && UNITY_IOS
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;

namespace SeongbukAR.Editor
{
    public static class ScanIOSBuildPostprocessor
    {
        [PostProcessBuild(1000)]
        public static void LinkVisionFrameworks(BuildTarget target, string buildPath)
        {
            if (target != BuildTarget.iOS) return;
            string path = PBXProject.GetPBXProjectPath(buildPath);
            var project = new PBXProject();
            project.ReadFromFile(path);
            string frameworkTarget = project.GetUnityFrameworkTargetGuid();
            project.AddFrameworkToProject(frameworkTarget, "Vision.framework", false);
            project.AddFrameworkToProject(frameworkTarget, "ImageIO.framework", false);
            project.AddFrameworkToProject(frameworkTarget, "CoreGraphics.framework", false);
            bool nativeSourceFound = false;
            foreach (string source in Directory.GetFiles(buildPath, "SeongbukVision.mm", SearchOption.AllDirectories))
            {
                string relative = Path.GetRelativePath(buildPath, source).Replace('\\', '/');
                string fileGuid = project.FindFileGuidByRealPath(relative);
                if (string.IsNullOrEmpty(fileGuid)) fileGuid = project.FindFileGuidByProjectPath(relative);
                if (string.IsNullOrEmpty(fileGuid)) continue;
                var flags = project.GetCompileFlagsForFile(frameworkTarget, fileGuid) ?? new List<string>();
                flags.RemoveAll(flag => flag == "-fno-objc-arc");
                if (!flags.Contains("-fobjc-arc")) flags.Add("-fobjc-arc");
                project.SetCompileFlagsForFile(frameworkTarget, fileGuid, flags);
                nativeSourceFound = true;
            }
            if (!nativeSourceFound)
                throw new BuildFailedException("SeongbukVision.mm was not found in the generated Xcode project. Check the iOS plug-in import settings.");
            project.WriteToFile(path);
        }
    }
}
#endif
