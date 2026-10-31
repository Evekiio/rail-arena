using System.Collections;
using UnityEngine.TestTools;

namespace RailArena.Tests
{
    public sealed class StandingCandidateArtReview
    {
        readonly CrouchCandidateArtReview review = new();

        [UnityTest]
        public IEnumerator FourCharactersKeepPlantedFeetAndWeaponContacts()
            => review.Capture(false, new[] { "StandingReady" }, "../Validation/StandingCandidatePlayback");

        [UnityTest]
        public IEnumerator InstalledIdleKeepsPlantedFeetAndWeaponContacts()
            => review.Capture(true, new[] { "StandingReady" }, "../Validation/StandingInstalledPlayback", true);

        [UnityTearDown]
        public IEnumerator Cleanup() => review.Cleanup();
    }
}
