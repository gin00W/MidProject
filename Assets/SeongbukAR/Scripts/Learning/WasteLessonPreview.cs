using UnityEngine;

namespace SeongbukAR
{
    // Explicitly labelled Editor demonstration, not simulated image tracking.
    public sealed class WasteLessonPreview : MonoBehaviour
    {
        WasteLessonVisual _visual;

        public void Configure(Camera camera)
        {
            if (camera == null) return;
            // Leave room below the model for the step controls in portrait.
            camera.transform.SetPositionAndRotation(new Vector3(0.16f, 0.135f, -0.35f), Quaternion.identity);
            camera.transform.LookAt(new Vector3(0, 0.02f, 0));
            camera.nearClipPlane = 0.005f;
            camera.farClipPlane = 30;
            camera.fieldOfView = 45;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.94f, 0.95f, 0.96f);
            var model = new GameObject("Recycling Model Preview");
            model.transform.SetParent(transform, false);
            _visual = model.AddComponent<WasteLessonVisual>();
            _visual.Configure(camera);
        }

        public void Show(string itemId, int stepIndex, bool visible)
        {
            if (_visual == null) return;
            _visual.gameObject.SetActive(visible && itemId != null);
            if (visible && itemId != null) _visual.Show(itemId, stepIndex);
        }
    }
}
