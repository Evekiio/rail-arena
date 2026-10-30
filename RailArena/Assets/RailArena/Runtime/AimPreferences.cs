using UnityEngine;

namespace RailArena
{
    public sealed class AimPreferences
    {
        public const float MinimumSensitivity = .02f, MaximumSensitivity = .5f;
        public const float MinimumFov = 70, MaximumFov = 110;
        private float sensitivity = .12f, zoomSensitivity = .12f, fieldOfView = 100;
        public float Sensitivity { get => sensitivity; set => sensitivity = Bounded(value, .12f, MinimumSensitivity, MaximumSensitivity); }
        public float ZoomSensitivity { get => zoomSensitivity; set => zoomSensitivity = Bounded(value, .12f, MinimumSensitivity, MaximumSensitivity); }
        public float FieldOfView { get => fieldOfView; set => fieldOfView = Bounded(value, 100, MinimumFov, MaximumFov); }
        public bool InvertY { get; set; }

        public void Load()
        {
            Sensitivity = PlayerPrefs.GetFloat("sensitivity", .12f);
            ZoomSensitivity = PlayerPrefs.GetFloat("zoomSensitivity", Sensitivity);
            FieldOfView = PlayerPrefs.GetFloat("fieldOfView", 100);
            InvertY = PlayerPrefs.GetInt("invertY", 0) != 0;
        }

        public void Save()
        {
            PlayerPrefs.SetFloat("sensitivity", Sensitivity);
            PlayerPrefs.SetFloat("zoomSensitivity", ZoomSensitivity);
            PlayerPrefs.SetFloat("fieldOfView", FieldOfView);
            PlayerPrefs.SetInt("invertY", InvertY ? 1 : 0);
        }

        public float ViewFov(bool zoom)
            => zoom ? 2 * Mathf.Atan(Mathf.Tan(FieldOfView * .5f * Mathf.Deg2Rad) / 1.5f) * Mathf.Rad2Deg : FieldOfView;

        public Vector2 LookDelta(Vector2 mouseDelta, bool zoom)
        {
            var delta = mouseDelta * (zoom ? ZoomSensitivity : Sensitivity);
            return new Vector2(delta.x, InvertY ? -delta.y : delta.y);
        }

        private static float Bounded(float value, float fallback, float min, float max)
            => float.IsNaN(value) || float.IsInfinity(value) ? fallback : Mathf.Clamp(value, min, max);
    }
}
