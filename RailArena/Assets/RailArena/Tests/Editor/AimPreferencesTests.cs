using NUnit.Framework;
using UnityEngine;

namespace RailArena.Tests
{
    public sealed class AimPreferencesTests
    {
        private readonly string[] floatKeys = { "sensitivity", "zoomSensitivity", "fieldOfView" };
        private readonly bool[] present = new bool[4];
        private readonly float[] original = new float[3];
        private int invert;

        [SetUp]
        public void PreservePreferences()
        {
            for (int i = 0; i < 3; i++)
            {
                present[i] = PlayerPrefs.HasKey(floatKeys[i]);
                original[i] = PlayerPrefs.GetFloat(floatKeys[i]);
                PlayerPrefs.DeleteKey(floatKeys[i]);
            }
            present[3] = PlayerPrefs.HasKey("invertY"); invert = PlayerPrefs.GetInt("invertY");
            PlayerPrefs.DeleteKey("invertY");
        }

        [TearDown]
        public void RestorePreferences()
        {
            for (int i = 0; i < 3; i++)
                if (present[i]) PlayerPrefs.SetFloat(floatKeys[i], original[i]); else PlayerPrefs.DeleteKey(floatKeys[i]);
            if (present[3]) PlayerPrefs.SetInt("invertY", invert); else PlayerPrefs.DeleteKey("invertY");
            PlayerPrefs.Save();
        }

        [Test]
        public void LegacySensitivityBecomesInitialZoomValueUntilIndependentlySaved()
        {
            PlayerPrefs.SetFloat("sensitivity", .23f);
            var aim = new AimPreferences(); aim.Load();
            Assert.That(aim.Sensitivity, Is.EqualTo(.23f));
            Assert.That(aim.ZoomSensitivity, Is.EqualTo(.23f));
            Assert.That(aim.FieldOfView, Is.EqualTo(100));
            aim.ZoomSensitivity = .08f; aim.FieldOfView = 84; aim.InvertY = true; aim.Save();
            var restored = new AimPreferences(); restored.Load();
            Assert.That(restored.Sensitivity, Is.EqualTo(.23f));
            Assert.That(restored.ZoomSensitivity, Is.EqualTo(.08f));
            Assert.That(restored.FieldOfView, Is.EqualTo(84));
            Assert.That(restored.InvertY, Is.True);
        }

        [TestCase(70)] [TestCase(84)] [TestCase(100)] [TestCase(110)]
        public void EveryFovRetainsOneAndAHalfTimesMagnification(float fov)
        {
            var aim = new AimPreferences { FieldOfView = fov };
            float ratio = Mathf.Tan(fov * .5f * Mathf.Deg2Rad) / Mathf.Tan(aim.ViewFov(true) * .5f * Mathf.Deg2Rad);
            Assert.That(ratio, Is.EqualTo(1.5f).Within(.0001f));
            Assert.That(aim.ViewFov(false), Is.EqualTo(fov));
        }

        [Test]
        public void InvalidSavedValuesCannotPoisonCameraOrLook()
        {
            PlayerPrefs.SetFloat("sensitivity", float.NaN);
            PlayerPrefs.SetFloat("zoomSensitivity", float.PositiveInfinity);
            PlayerPrefs.SetFloat("fieldOfView", 999);
            var aim = new AimPreferences(); aim.Load();
            Assert.That(aim.Sensitivity, Is.EqualTo(.12f));
            Assert.That(aim.ZoomSensitivity, Is.EqualTo(.12f));
            Assert.That(aim.FieldOfView, Is.EqualTo(110));
            aim.FieldOfView = float.NegativeInfinity;
            Assert.That(aim.FieldOfView, Is.EqualTo(100));
        }

        [Test]
        public void InversionChangesOnlyVerticalLookAndZoomHasItsOwnSensitivity()
        {
            var aim = new AimPreferences { Sensitivity = .2f, ZoomSensitivity = .1f };
            Assert.That(aim.LookDelta(new Vector2(10, 20), false), Is.EqualTo(new Vector2(2, 4)));
            Assert.That(aim.LookDelta(new Vector2(10, 20), true), Is.EqualTo(new Vector2(1, 2)));
            aim.InvertY = true;
            Assert.That(aim.LookDelta(new Vector2(10, 20), true), Is.EqualTo(new Vector2(1, -2)));
        }
    }
}
