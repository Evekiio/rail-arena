using System.Linq;
using NUnit.Framework;
using RailArena.Simulation;
using UnityEngine;

namespace RailArena.Tests
{
    public sealed class OfflineRosterTests
    {
        [TestCase(0)] [TestCase(1)] [TestCase(2)] [TestCase(3)]
        public void EverySelectedCharacterHasACompleteNonrepeatingRoster(int selected)
        {
            var roster = Enumerable.Range(0,CombatRules.MaximumContenders)
                .Select(actor => OfflineRoster.CharacterForActor(selected,actor)).ToArray();
            Assert.That(roster[0], Is.EqualTo(selected));
            CollectionAssert.AreEquivalent(new[] {0,1,2,3},roster);
            Assert.That(roster.Select(OfflineRoster.PrefabPath).Distinct().Count(), Is.EqualTo(4));
            foreach (int character in roster)
            {
                var prefab = Resources.Load<GameObject>(OfflineRoster.PrefabPath(character));
                Assert.That(prefab, Is.Not.Null);
                Assert.That(prefab.GetComponent<ContenderPresentation>().Animator.avatar.isHuman, Is.True);
            }
        }

        [Test]
        public void CharacterChoiceRetainsTheSameDamageColliderDimensions()
        {
            var reference = Resources.Load<GameObject>(OfflineRoster.PrefabPath(0)).GetComponentsInChildren<Hitbox>(true)
                .OrderBy(hitbox => hitbox.name).ToArray();
            for (int character = 1; character < 4; character++)
            {
                var actual = Resources.Load<GameObject>(OfflineRoster.PrefabPath(character)).GetComponentsInChildren<Hitbox>(true)
                    .OrderBy(hitbox => hitbox.name).ToArray();
                Assert.That(actual.Length, Is.EqualTo(reference.Length));
                for (int i = 0; i < reference.Length; i++)
                {
                    Assert.That(actual[i].name, Is.EqualTo(reference[i].name));
                    Assert.That(actual[i].Region, Is.EqualTo(reference[i].Region));
                    var first = reference[i].GetComponent<Collider>(); var other = actual[i].GetComponent<Collider>();
                    Assert.That(other.GetType(), Is.EqualTo(first.GetType()));
                    Assert.That(other.transform.lossyScale, Is.EqualTo(first.transform.lossyScale));
                    if (first is CapsuleCollider capsule && other is CapsuleCollider match)
                    {
                        Assert.That(match.radius, Is.EqualTo(capsule.radius));
                        Assert.That(match.height, Is.EqualTo(capsule.height));
                        Assert.That(match.direction, Is.EqualTo(capsule.direction));
                        Assert.That(match.center, Is.EqualTo(capsule.center));
                    }
                    else if (first is SphereCollider sphere && other is SphereCollider matchSphere)
                    {
                        Assert.That(matchSphere.radius, Is.EqualTo(sphere.radius));
                        Assert.That(matchSphere.center, Is.EqualTo(sphere.center));
                    }
                    else if (first is BoxCollider box && other is BoxCollider matchBox)
                    {
                        Assert.That(matchBox.size, Is.EqualTo(box.size));
                        Assert.That(matchBox.center, Is.EqualTo(box.center));
                    }
                    else Assert.Fail("Unqualified damage collider type: " + first.GetType().Name);
                }
            }
        }

        [Test]
        public void MatchAuthorityRejectsOverCapacityInBothOfflineModes()
        {
            foreach (bool practice in new[] {false,true})
            {
                Assert.DoesNotThrow(() => new MatchState(new[] {0,1,2,3},practice));
                Assert.Throws<System.ArgumentException>(() => new MatchState(new[] {0,1,2,3,4},practice));
            }
            Assert.Throws<System.ArgumentOutOfRangeException>(() => OfflineRoster.CharacterForActor(0,4));
            Assert.That(OfflineRoster.NormalizeCharacter(-1), Is.EqualTo(0));
            Assert.That(OfflineRoster.NormalizeCharacter(99), Is.EqualTo(3));
        }
    }
}
