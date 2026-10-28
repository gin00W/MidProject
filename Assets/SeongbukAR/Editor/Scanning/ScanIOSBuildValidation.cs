#if UNITY_EDITOR && UNITY_IOS
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace SeongbukAR.Editor
{
    // Catch a camera-pose configuration problem before exporting/building Xcode.
    public sealed class ScanIOSBuildValidation : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            if (report.summary.platform != BuildTarget.iOS) return;
#if !ENABLE_INPUT_SYSTEM
            throw new BuildFailedException("아이폰 AR 카메라 추적을 위해 Player > Other Settings > Active Input Handling을 Input System Package (New) 또는 Both로 바꾸고 Unity를 재시작해 주세요.");
#else
            if (string.IsNullOrWhiteSpace(PlayerSettings.iOS.cameraUsageDescription))
                throw new BuildFailedException("Player > iOS > Other Settings > Camera Usage Description에 물건 스캔을 위한 카메라 사용 이유를 입력해 주세요.");
#endif
        }
    }
}
#endif
