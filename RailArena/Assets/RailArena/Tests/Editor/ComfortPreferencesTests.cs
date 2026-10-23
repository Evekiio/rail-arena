using NUnit.Framework;
using UnityEngine;

namespace RailArena.Tests
{
    public sealed class ComfortPreferencesTests
    {
        private bool hadFlash, hadMotion;
        private int flash, motion;
        [SetUp]
        public void Preserve()
        {
            hadFlash=PlayerPrefs.HasKey(ComfortPreferences.FlashKey); hadMotion=PlayerPrefs.HasKey(ComfortPreferences.MotionKey);
            flash=PlayerPrefs.GetInt(ComfortPreferences.FlashKey); motion=PlayerPrefs.GetInt(ComfortPreferences.MotionKey);
            PlayerPrefs.DeleteKey(ComfortPreferences.FlashKey); PlayerPrefs.DeleteKey(ComfortPreferences.MotionKey);
        }
        [TearDown]
        public void Restore()
        {
            if(hadFlash)PlayerPrefs.SetInt(ComfortPreferences.FlashKey,flash); else PlayerPrefs.DeleteKey(ComfortPreferences.FlashKey);
            if(hadMotion)PlayerPrefs.SetInt(ComfortPreferences.MotionKey,motion); else PlayerPrefs.DeleteKey(ComfortPreferences.MotionKey);
            PlayerPrefs.Save();
        }
        [Test]
        public void ExistingPresentationIsDefaultAndChoicesPersistIndependently()
        {
            var preferences=new ComfortPreferences(); preferences.Load();
            Assert.That(preferences.ReducedFlash,Is.False); Assert.That(preferences.WeaponMotion,Is.True);
            preferences.ReducedFlash=true; preferences.WeaponMotion=false; preferences.Save();
            var restored=new ComfortPreferences(); restored.Load();
            Assert.That(restored.ReducedFlash,Is.True); Assert.That(restored.WeaponMotion,Is.False);
            Assert.That(restored.DamageEdgeOpacity,Is.EqualTo(.175f));
            restored.ReducedFlash=false; restored.Save(); preferences.Load();
            Assert.That(preferences.ReducedFlash,Is.False); Assert.That(preferences.WeaponMotion,Is.False);
        }
        [Test]
        public void UnknownStoredFlagsUseNormalPresentation()
        {
            PlayerPrefs.SetInt(ComfortPreferences.FlashKey,-99); PlayerPrefs.SetInt(ComfortPreferences.MotionKey,-99);
            var preferences=new ComfortPreferences(); preferences.Load();
            Assert.That(preferences.ReducedFlash,Is.False); Assert.That(preferences.WeaponMotion,Is.True);
        }
    }
}
