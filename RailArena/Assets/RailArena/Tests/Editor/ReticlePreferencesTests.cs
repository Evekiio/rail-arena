using NUnit.Framework;
using UnityEngine;

namespace RailArena.Tests
{
    public sealed class ReticlePreferencesTests
    {
        private bool hadSize, hadColor;
        private float originalSize;
        private int originalColor;

        [SetUp]
        public void Preserve()
        {
            hadSize=PlayerPrefs.HasKey(ReticlePreferences.SizeKey); hadColor=PlayerPrefs.HasKey(ReticlePreferences.ColorKey);
            originalSize=PlayerPrefs.GetFloat(ReticlePreferences.SizeKey); originalColor=PlayerPrefs.GetInt(ReticlePreferences.ColorKey);
            PlayerPrefs.DeleteKey(ReticlePreferences.SizeKey); PlayerPrefs.DeleteKey(ReticlePreferences.ColorKey);
        }

        [TearDown]
        public void Restore()
        {
            if(hadSize)PlayerPrefs.SetFloat(ReticlePreferences.SizeKey,originalSize); else PlayerPrefs.DeleteKey(ReticlePreferences.SizeKey);
            if(hadColor)PlayerPrefs.SetInt(ReticlePreferences.ColorKey,originalColor); else PlayerPrefs.DeleteKey(ReticlePreferences.ColorKey);
            PlayerPrefs.Save();
        }

        [Test]
        public void DefaultsAndAllPaletteChoicesRoundTripAndReset()
        {
            var preferences=new ReticlePreferences(); preferences.Load();
            Assert.That(preferences.Size,Is.EqualTo(36)); Assert.That(preferences.ColorName,Is.EqualTo("CYAN"));
            for(int i=0;i<ReticlePreferences.ColorCount;i++)
            {
                preferences.ColorIndex=i; preferences.Size=64; preferences.Save();
                var restored=new ReticlePreferences(); restored.Load();
                Assert.That(restored.ColorIndex,Is.EqualTo(i)); Assert.That(restored.Color,Is.EqualTo(preferences.Color));
                Assert.That(restored.Size,Is.EqualTo(64)); Assert.That(restored.Color.a,Is.EqualTo(1));
            }
            preferences.Reset(); preferences.Save();
            var reset=new ReticlePreferences(); reset.Load();
            Assert.That(reset.Size,Is.EqualTo(36)); Assert.That(reset.ColorIndex,Is.Zero);
        }

        [Test]
        public void CorruptPreferencesCannotHideOrExpandTheReticleWithoutBounds()
        {
            var preferences=new ReticlePreferences();
            foreach(float invalid in new[]{float.NaN,float.PositiveInfinity,float.NegativeInfinity})
            {
                PlayerPrefs.SetFloat(ReticlePreferences.SizeKey,invalid); preferences.Load();
                Assert.That(preferences.Size,Is.EqualTo(36));
            }
            preferences.Size=-100; Assert.That(preferences.Size,Is.EqualTo(18));
            preferences.Size=10000; Assert.That(preferences.Size,Is.EqualTo(64));
            foreach(int invalid in new[]{-1,5,int.MaxValue})
            {
                PlayerPrefs.SetInt(ReticlePreferences.ColorKey,invalid); preferences.Load();
                Assert.That(preferences.ColorName,Is.EqualTo("CYAN"));
            }
        }
    }
}
