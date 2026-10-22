using NUnit.Framework;
using UnityEngine;

namespace RailArena.Tests
{
    public sealed class FirstPersonWeaponMotionTests
    {
        [Test]
        public void LandingUsesFallSpeedAndResetPreventsPhantomImpact()
        {
            var motion = new FirstPersonWeaponMotion();
            motion.Reset(false);
            motion.Step(1f / 60, Vector3.down * 20, false, Vector2.zero, 0);
            float lowest = 0;
            for (int frame = 0; frame < 60; frame++)
            {
                motion.Step(1f / 60, Vector3.down, true, Vector2.zero, 0);
                lowest = Mathf.Min(lowest, motion.Offset.y);
            }
            Assert.That(lowest, Is.InRange(-.0401f, -.035f));
            Assert.That(motion.Offset.magnitude, Is.LessThan(.0001f));
            motion.Step(1f / 60, Vector3.down * 40, false, Vector2.zero, 0);
            motion.Reset(false);
            motion.Step(1f / 60, Vector3.zero, true, Vector2.zero, 0);
            Assert.That(motion.Offset, Is.EqualTo(Vector3.zero));
        }

        [Test]
        public void ExtremeTravelAndLookStayBoundedAndSettleAtRest()
        {
            var motion = new FirstPersonWeaponMotion();
            motion.Reset(true);
            for (int frame = 0; frame < 1000; frame++)
            {
                motion.Step(1f / 144, Vector3.forward * 1000, true, new Vector2(1000, -1000), 1);
                Assert.That(Mathf.Abs(motion.Offset.x), Is.LessThanOrEqualTo(.0121f));
                Assert.That(Mathf.Abs(motion.Offset.y), Is.LessThan(.18f));
                Assert.That(Mathf.Abs(motion.Offset.z), Is.LessThan(.036f));
                Assert.That(Quaternion.Angle(Quaternion.identity, motion.Rotation), Is.LessThan(15));
            }
            for (int frame = 0; frame < 288; frame++) motion.Step(1f / 144, Vector3.zero, true, Vector2.zero, 0);
            Assert.That(motion.Offset.magnitude, Is.LessThan(.00001f));
            Assert.That(Quaternion.Angle(Quaternion.identity, motion.Rotation), Is.LessThan(.001f));
        }

        [Test]
        public void LookSwayConvergesEquallyAcrossRenderRates()
        {
            Quaternion Sample(int fps)
            {
                var motion = new FirstPersonWeaponMotion();
                for (int frame = 0; frame < fps; frame++)
                    motion.Step(1f / fps, Vector3.zero, true, new Vector2(180, -60) / fps, 0);
                return motion.Rotation;
            }
            Assert.That(Quaternion.Angle(Sample(30), Sample(144)), Is.LessThan(.01f));
        }
    }
}
