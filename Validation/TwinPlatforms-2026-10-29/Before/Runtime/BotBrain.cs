using RailArena.Simulation;
using UnityEngine;

namespace RailArena
{
    public enum TargetScenario { Combat, Stationary, Moving }

    public sealed class BotBrain
    {
        private readonly System.Random random;
        private ArenaActor target;
        private int visibleSince;
        private int decisionTick;
        private int goal;
        private int next = -1;
        private Vector3 lastPosition;
        private int stuckTicks;
        private float yaw;
        private float pitch;
        private float aimYawError;
        private float aimPitchError;
        private bool landing;
        public int NodesReached { get; private set; }

        public BotBrain(int seed) { random = new System.Random(seed); }

        public void Launched(int landingNode) { next = goal = landingNode; landing = true; }

        public MovementCommand Think(ArenaSession session, ArenaActor self)
        {
            if (session.Scenario == TargetScenario.Stationary) return default;
            int tick = session.Match.Tick;
            var tuning = session.BotSettings;
            var world = session.World;
            if (tick >= decisionTick)
            {
                decisionTick = tick + tuning.DecisionIntervalTicks;
                if (next < 0) goal = random.Next(world.Nodes.Length);
                if (self.Health <= tuning.SeekHealthAtOrBelow)
                {
                    float nearest = float.MaxValue;
                    foreach (var pickup in world.Pickups)
                    {
                        float distance = (pickup.transform.position - self.transform.position).sqrMagnitude;
                        if (pickup.AvailableTick <= tick && distance < nearest)
                        { nearest = distance; goal = world.NearestNode(pickup.transform.position); }
                    }
                }
                int current = world.NearestNode(self.transform.position);
                // Keep a destination until reached; rerouting each frame oscillates at junctions.
                if (next < 0 || next >= world.Nodes.Length || stuckTicks > 100)
                    next = current;
            }

            if (target == null || !target.Alive || !Visible(self, target))
            {
                target = null;
                visibleSince = tick;
                float best = float.MaxValue;
                foreach (var actor in session.Actors)
                {
                    if (actor == self || !actor.Alive) continue;
                    float distance = (actor.Eye - self.Eye).sqrMagnitude;
                    if (distance < best && Visible(self, actor)) { target = actor; best = distance; }
                }
            }

            Vector3 destination = world.Nodes[next].position;
            Vector3 offset = destination - self.transform.position;
            Vector3 flat = new(offset.x, 0, offset.z);
            if (landing && self.Controller.isGrounded && self.AirborneTicks == 0)
            {
                // Brake on the landing deck using ordinary ground friction before choosing a turn.
                if (new Vector2(self.Velocity.x, self.Velocity.z).magnitude > 2)
                    return new MovementCommand { Yaw = yaw, Pitch = pitch };
                landing = false;
            }
            if (!landing && flat.magnitude < 1.4f && Mathf.Abs(offset.y) < 2)
            {
                NodesReached++;
                int current = next;
                if (current == goal) goal = random.Next(world.Nodes.Length);
                next = world.NextNode(current, goal);
            }
            if ((lastPosition - self.transform.position).sqrMagnitude < .0005f) stuckTicks++;
            else stuckTicks = 0;
            lastPosition = self.transform.position;

            Vector3 travel = flat.normalized;
            bool fire = false;
            if (target != null && session.Scenario == TargetScenario.Combat)
            {
                Vector3 direction = (target.Eye - Vector3.up * tuning.AimBelowEyeMetres - self.Eye).normalized;
                float wantedYaw = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
                float wantedPitch = -Mathf.Asin(direction.y) * Mathf.Rad2Deg;
                float turn = tuning.TurnDegreesPerSecond / CombatRules.TickRate;
                if (tick % tuning.AimRefreshTicks == 0)
                {
                    aimYawError = ((float)random.NextDouble() * 2 - 1) * tuning.AimErrorDegrees;
                    aimPitchError = ((float)random.NextDouble() * 2 - 1) * tuning.AimErrorDegrees;
                }
                yaw = Mathf.MoveTowardsAngle(yaw, wantedYaw + aimYawError, turn);
                pitch = Mathf.MoveTowardsAngle(pitch, wantedPitch + aimPitchError, turn);
                fire = tick - visibleSince >= tuning.ReactionTicks &&
                    Mathf.Abs(Mathf.DeltaAngle(yaw, wantedYaw)) < tuning.FireYawToleranceDegrees;
                // Keep moving along safe routes while aiming independently.
            }
            else
            {
                yaw = Mathf.Atan2(travel.x, travel.z) * Mathf.Rad2Deg;
                pitch = 0;
            }
            Vector3 local = Quaternion.Euler(0, -yaw, 0) * travel;
            return new MovementCommand { Move = new Vector2(local.x, local.z), Yaw = yaw, Pitch = pitch,
                Jump = stuckTicks > 30, Fire = fire, Reload = self.Magazine.Rounds == 0 };
        }

        private static bool Visible(ArenaActor self, ArenaActor other)
        {
            Vector3 direction = other.Eye - self.Eye;
            return !Physics.Raycast(self.Eye, direction.normalized, direction.magnitude, 1 << 0,
                QueryTriggerInteraction.Ignore);
        }
    }
}
