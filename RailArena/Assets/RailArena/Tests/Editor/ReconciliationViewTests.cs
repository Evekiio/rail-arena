using NUnit.Framework;
using RailArena.Networking;
using UnityEngine;

namespace RailArena.Tests
{
    public sealed class ReconciliationViewTests
    {
        [Test]
        public void CorrectionPreservesViewWithoutMovingCorrectedActorPosition()
        {
            var view = new ReconciliationView();
            var corrected = new Vector3(1000, 2, 1000);
            var previous = corrected + new Vector3(.5f, 1, 0);
            view.Correct(previous, corrected);
            Assert.That(view.Position(corrected), Is.EqualTo(previous));
            Assert.That(corrected, Is.EqualTo(new Vector3(1000, 2, 1000)));
        }

        [Test]
        public void ExponentialDecayIsIndependentOfFramePartitionAndSettles()
        {
            var single = new ReconciliationView(); var divided = new ReconciliationView();
            single.Correct(Vector3.one, Vector3.zero); divided.Correct(Vector3.one, Vector3.zero);
            single.Decay(.1f);
            for (int i = 0; i < 10; i++) divided.Decay(.01f);
            Assert.That(Vector3.Distance(single.Offset, divided.Offset), Is.LessThan(.00001f));
            single.Decay(1);
            Assert.That(single.Offset.magnitude, Is.LessThan(.00001f));
        }

        [Test]
        public void TeleportsAndExplicitLifeChangesResetCameraOffset()
        {
            var view = new ReconciliationView();
            view.Correct(Vector3.one, Vector3.zero); view.Reset();
            Assert.That(view.Offset, Is.EqualTo(Vector3.zero));
            view.Correct(Vector3.right * 10, Vector3.zero);
            Assert.That(view.Offset, Is.EqualTo(Vector3.zero));
        }

        [Test]
        public void CameraOffsetCannotCrossStaticWall()
        {
            var wall = new GameObject("View correction wall");
            try
            {
                var eye = new Vector3(1000, 2, 1000);
                wall.transform.position = eye + Vector3.forward * .75f;
                wall.AddComponent<BoxCollider>().size = new Vector3(2, 2, .2f);
                Physics.SyncTransforms();
                var view = new ReconciliationView(); view.Correct(eye + Vector3.forward, eye);
                Assert.That(view.Position(eye).z, Is.EqualTo(1000.6f).Within(.001f));
            }
            finally { Object.DestroyImmediate(wall); }
        }
    }
}
