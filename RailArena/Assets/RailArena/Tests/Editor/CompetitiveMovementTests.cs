using NUnit.Framework;
using RailArena.Networking;
using RailArena.Simulation;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RailArena.Tests
{
    public sealed class CompetitiveMovementTests
    {
        [Test]
        public void FirstPersonOpticUsesOwnedMeshesAndHidesExteriorGlass()
        {
            var prefab = Resources.Load<GameObject>("Weapons/Railgun");
            var root = Object.Instantiate(prefab);
            try
            {
                var weapon = root.GetComponent<RailgunPresentation>();
                var original = System.Array.Find(prefab.GetComponentsInChildren<MeshFilter>(), filter => filter.name == "RF_07_shell_Surface").sharedMesh;
                int originalIndices = original.triangles.Length;
                weapon.ConfigureFirstPerson();
                int opened = 0;
                foreach (var filter in root.GetComponentsInChildren<MeshFilter>())
                {
                    if (filter.name == "RF_07_shell_Energy")
                        Assert.That(filter.GetComponent<Renderer>().forceRenderingOff, Is.True);
                    if (filter.name == "RF_06_shell_Energy")
                        Assert.That(filter.GetComponent<Renderer>().forceRenderingOff, Is.False, "Muzzle energy must remain visible.");
                    if (filter.name == "RF_07_shell_Surface")
                    {
                        Assert.That(filter.sharedMesh.name, Is.EqualTo("First-person open optic"));
                        Assert.That(filter.sharedMesh.triangles.Length, Is.LessThan(originalIndices));
                        opened++;
                    }
                }
                Assert.That(opened, Is.EqualTo(3));
                Assert.That(original.triangles.Length, Is.EqualTo(originalIndices));
                foreach (var filter in prefab.GetComponentsInChildren<MeshFilter>())
                    Assert.That(filter.sharedMesh.name, Is.Not.EqualTo("First-person open optic"), "Source prefab meshes remain untouched.");
            }
            finally { Object.DestroyImmediate(root); }
        }

        [Test]
        public void SpeedModesRespectPrecedenceWithoutChangingBaseMovementOrAirMomentum()
        {
            var tuning = ScriptableObject.CreateInstance<MovementTuning>();
            try
            {
                float Speed(MovementCommand command)
                {
                    Vector3 velocity = Vector3.zero;
                    for (int i = 0; i < 180; i++) velocity = ArenaMovement.Integrate(velocity, command, true, tuning, 1f / 60);
                    return new Vector2(velocity.x, velocity.z).magnitude;
                }
                Assert.That(Speed(new MovementCommand { Move = Vector2.up }), Is.EqualTo(8).Within(.01f));
                Assert.That(Speed(new MovementCommand { Move = Vector2.up, Sprint = true }), Is.EqualTo(10.8f).Within(.01f));
                Assert.That(Speed(new MovementCommand { Move = Vector2.down, Sprint = true }), Is.EqualTo(8).Within(.01f));
                Assert.That(Speed(new MovementCommand { Move = Vector2.one, Sprint = true, Walk = true }), Is.EqualTo(4).Within(.01f));
                Assert.That(Speed(new MovementCommand { Move = Vector2.up, Sprint = true, Walk = true, Crouch = true }), Is.EqualTo(3.6f).Within(.01f));
                var air = ArenaMovement.Integrate(new Vector3(20, 3, 0), new MovementCommand { Crouch = true }, false, tuning, 1f / 60);
                Assert.That(air.x, Is.EqualTo(20));
                var slow = ArenaMovement.Integrate(new Vector3(20, 0, 0), new MovementCommand { Walk = true }, true, tuning, 1f / 60);
                Assert.That(slow.x, Is.LessThanOrEqualTo(4));
            }
            finally { Object.DestroyImmediate(tuning); }
        }

        [Test]
        public void CeilingKeepsCrouchUntilClearAndPlacementResetsStance()
        {
            var root = new GameObject("Crouch clearance test") { layer = 9 };
            var roof = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var tuning = ScriptableObject.CreateInstance<MovementTuning>();
            try
            {
                root.transform.position = new Vector3(3000, 100, 3000);
                var actor = root.AddComponent<ArenaActor>();
                actor.Controller = root.AddComponent<CharacterController>();
                actor.Controller.radius = .36f;
                actor.SetCrouched(true);
                roof.transform.position = root.transform.position + Vector3.up * 1.5f;
                roof.transform.localScale = new Vector3(3, .1f, 3);
                Physics.SyncTransforms();
                Assert.That(actor.CanStand(), Is.False);
                actor.Command = default;
                actor.Move(tuning);
                Assert.That(actor.IsCrouched, Is.True);
                Assert.That(actor.Controller.height, Is.EqualTo(1.2f));
                Assert.That(actor.Eye.y - root.transform.position.y, Is.EqualTo(.98f).Within(.001f));
                Object.DestroyImmediate(roof); Physics.SyncTransforms();
                actor.Move(tuning);
                Assert.That(actor.IsCrouched, Is.False);
                Assert.That(actor.Controller.height, Is.EqualTo(1.8f));
                actor.SetCrouched(true); actor.Place(new Vector3(3000, 100, 3000));
                Assert.That(actor.IsCrouched, Is.False);
                Assert.That(actor.Eye.y - root.transform.position.y, Is.EqualTo(1.58f).Within(.001f));
            }
            finally { Object.DestroyImmediate(root); if (roof != null) Object.DestroyImmediate(roof); Object.DestroyImmediate(tuning); }
        }

        [Test]
        public void WireRoundTripsAllMovementFlagsAndAuthoritativeCrouchAndRejectsUnknownFlags()
        {
            var command = new MovementCommand { Sequence = 1, Move = Vector2.up, Sprint = true, Walk = true, Crouch = true };
            var bytes = DuelProtocol.Encode(new DuelPacket { Kind = DuelMessage.Input,
                Inputs = new[] { new DuelInput { Command = command } } });
            Assert.That(DuelProtocol.TryDecode(bytes, out var decoded), Is.True);
            var actual = decoded.Inputs[0].Command;
            Assert.That(actual.Sprint && actual.Walk && actual.Crouch, Is.True);
            bytes[bytes.Length - 1] = 64;
            Assert.That(DuelProtocol.TryDecode(bytes, out _), Is.False);
            var state = new DuelSnapshot();
            state.Actors[0] = new ActorSnapshot { Life = 1, Health = 100, Crouched = true };
            state.Actors[1] = new ActorSnapshot { Life = 1, Health = 100 };
            bytes = DuelProtocol.Encode(new DuelPacket { Kind = DuelMessage.Snapshot, Snapshot = state });
            Assert.That(DuelProtocol.TryDecode(bytes, out decoded), Is.True);
            Assert.That(decoded.Snapshot.Actors[0].Crouched, Is.True);
            bytes[4] = 4; bytes[5] = 0;
            Assert.That(DuelProtocol.TryDecode(bytes, out _), Is.False);
        }

        [Test]
        public void AdsAlignsSightToCameraAndRemovesSwayWithoutChangingZoomContract()
        {
            var ads = new AimDownSights(); var sight = new Vector3(0, .115f, -.013f);
            ads.Step(true, .09f);
            Assert.That(ads.Blend, Is.EqualTo(.5f).Within(.001f));
            ads.Step(true, 1);
            var position = ads.Position(sight, new Vector3(.1f, .2f, .3f), 0);
            Assert.That(Vector3.Distance(position + sight, Vector3.forward * AimDownSights.EyeRelief), Is.LessThan(.0001f));
            Assert.That(Quaternion.Angle(ads.Rotation(Quaternion.Euler(10, 20, 30)), Quaternion.identity), Is.LessThan(.001f));
            var aim = new AimPreferences();
            Assert.That(Mathf.Tan(aim.FieldOfView * .5f * Mathf.Deg2Rad) / Mathf.Tan(aim.ViewFov(true) * .5f * Mathf.Deg2Rad), Is.EqualTo(1.5f).Within(.0001f));
            ads.Reset();
            Assert.That(ads.Blend, Is.Zero);
            Assert.That(ads.Position(sight, Vector3.zero, 0), Is.EqualTo(FirstPersonWeaponMotion.RestPosition));
        }
    }
}

