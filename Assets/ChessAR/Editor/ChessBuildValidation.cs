using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine.SceneManagement;

namespace ChessAR.Editor
{
    public sealed class ChessBuildValidation : IPreprocessBuildWithReport, IProcessSceneWithReport
    {
        public int callbackOrder => -2200;

        public void OnPreprocessBuild(BuildReport report)
        {
            if (report.summary.platform != BuildTarget.iOS) return;
#if !ENABLE_INPUT_SYSTEM
            throw new BuildFailedException("체스 AR 카메라 추적을 위해 Active Input Handling을 Input System Package (New) 또는 Both로 설정한 뒤 Unity를 재시작해 주세요.");
#else
            if (string.IsNullOrWhiteSpace(PlayerSettings.iOS.cameraUsageDescription))
                throw new BuildFailedException("Chess AR > 1. Create Chess Scenes를 실행해 카메라 사용 설명과 체스 빌드 장면을 준비해 주세요.");
#endif
        }

        public void OnProcessScene(Scene scene, BuildReport report)
        {
            // This inspects the actual scenes supplied by the active Build
            // Profile, including profiles with their own overridden Scene List.
            if (report == null || report.summary.platform != BuildTarget.iOS) return;
            if (scene.path != ChessSetup.ARScenePath)
                throw new BuildFailedException("체스 버전의 Scene List에는 Assets/ChessAR/Scenes/ChessAR.unity만 활성화해 주세요. 현재 포함된 장면: " + scene.path);
        }
    }
}
