using NUnit.Framework;

namespace RailArena.Tests
{
    public sealed class CombatFeedTests
    {
        [Test]
        public void KeepsGenuineRepeatedEventsAndExpiresAtTheLifetimeBoundary()
        {
            var feed=new CombatFeed();
            feed.Add("First",0);feed.Add("Repeated",1);feed.Add("Repeated",2);feed.Add("Newest",3);
            Assert.That(feed.Entries.Count,Is.EqualTo(3));Assert.That(feed.Entries[0].Text,Is.EqualTo("Repeated"));
            Assert.That(feed.Entries[1].Text,Is.EqualTo("Repeated"));Assert.That(feed.Entries[2].Text,Is.EqualTo("Newest"));
            feed.Prune(7);Assert.That(feed.Entries.Count,Is.EqualTo(2));
            feed.Prune(9);Assert.That(feed.Entries,Is.Empty);
            feed.Add("Reset",10);feed.Clear();Assert.That(feed.Entries,Is.Empty);
            feed.Add("",11);Assert.That(feed.Entries,Is.Empty);
        }
    }
}
