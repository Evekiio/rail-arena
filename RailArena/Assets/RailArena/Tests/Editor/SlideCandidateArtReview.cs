using System.Collections;
using UnityEngine.TestTools;

namespace RailArena.Tests
{
    /// <summary>Use the same full-skin/grip/root contract before installing new slide poses.</summary>
    public sealed class SlideCandidateArtReview
    {
        readonly CrouchCandidateArtReview review = new();

        [UnityTest]
        public IEnumerator CaptureNativeSlideCandidates()
            => review.Capture(false, new[] { "SlideEnter", "SlideHold", "SlideRecover" }, "../Validation/SlideCandidatePlayback");

        [UnityTearDown]
        public IEnumerator Cleanup() => review.Cleanup();
    }
}
