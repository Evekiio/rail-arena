using RailArena.Simulation;
using UnityEngine;

namespace RailArena
{
    /// <summary>Uses ordinary movement commands for the measured basement jumps and return devices.</summary>
    public sealed class BasementBotTraversal
    {
        enum Phase { Idle, Align, RunUp, FirstFlight, StageRun, SecondFlight, ReturnApproach, PortalCross, Returning }
        BasementRoute[] routes;
        BasementRoute route;
        Phase phase;
        int life, routeIndex, enteredTick;
        int portalSerial, padReady;
        public int TripsCompleted { get; private set; }
        public string Status => phase + " route=" + routeIndex;
        public int[] CompletedByRoute { get; } = new int[4];
        public bool Available => routes != null && routes.Length == 4;
        public bool TryThink(ArenaSession session, ArenaActor self, out MovementCommand command)
        {
            command = default;
            if (routes == null)
            {
                routes = session.World.GetComponentsInChildren<BasementRoute>();
                System.Array.Sort(routes, (a, b) => string.CompareOrdinal(a.name, b.name));
            }
            if (!Available) return false;
            int tick = session.Match.Tick;
            if (life != self.Life) { life = self.Life; phase = Phase.Idle; route = null; }
            var position = self.transform.position;
            if (phase == Phase.Idle)
            {
                if (self.Grounded && Mathf.Abs(position.y - 22.08f) < .2f)
                {
                    routeIndex = (self.Id + TripsCompleted) % routes.Length;
                    route = routes[routeIndex]; SetPhase(Phase.Align, tick);
                }
                else
                {
                    for (int i = 0; i < routes.Length; i++)
                    {
                        if ((position - (routes[i].Landing.position + Vector3.up * .38f)).sqrMagnitude < 40 && self.Grounded)
                        { routeIndex = i; route = routes[i]; SetPhase(Phase.ReturnApproach, tick); break; }
                        if ((position - (routes[i].Staging.position + Vector3.up * .38f)).sqrMagnitude < 12 && self.Grounded)
                        { routeIndex = i; route = routes[i]; SetPhase(Phase.StageRun, tick); break; }
                    }
                    if (phase == Phase.Idle) return false;
                }
                portalSerial = self.TeleportSerial; padReady = self.PadReadyTick;
            }
            // A completed teleport or pad return hands navigation back to the ordinary graph.
            if ((phase == Phase.ReturnApproach || phase == Phase.PortalCross || phase == Phase.Returning) &&
                ((self.TeleportSerial != portalSerial && position.y > -1) ||
                 (self.PadReadyTick != padReady && self.Grounded && position.y > -1)))
            {
                TripsCompleted++; CompletedByRoute[routeIndex]++; phase = Phase.Idle; route = null;
                return false;
            }
            if (phase == Phase.ReturnApproach && self.PadReadyTick != padReady) SetPhase(Phase.Returning, tick);
            if (phase == Phase.Returning) return true; // coast through the existing ballistic launch
            var direction = route.Direction;
            float radial = Vector3.Dot(position, direction);
            switch (phase)
            {
                case Phase.Align:
                    Vector3 current = new Vector3(position.x, 0, position.z).normalized;
                    if (current.sqrMagnitude < .1f) current = direction;
                    Vector3 waypoint = Vector3.RotateTowards(current, direction, 20 * Mathf.Deg2Rad, 0) * 4;
                    if (Vector3.Distance(new Vector3(position.x, 0, position.z), direction * 4) < .5f)
                        SetPhase(Phase.RunUp, tick);
                    command = Toward(position, waypoint + Vector3.up * position.y, false, true);
                    break;
                case Phase.RunUp:
                    command = Along(direction, true);
                    if (radial >= 6.1f && new Vector2(self.Velocity.x, self.Velocity.z).magnitude >= 10.2f)
                    { command.Jump = true; SetPhase(Phase.FirstFlight, tick); }
                    break;
                case Phase.FirstFlight:
                    command = Along(direction, true);
                    if (self.Grounded && Mathf.Abs(position.y - 14.08f) < .2f) SetPhase(Phase.StageRun, tick);
                    break;
                case Phase.StageRun:
                    command = Along(direction, true);
                    if (radial >= 23.2f && self.Grounded && new Vector2(self.Velocity.x, self.Velocity.z).magnitude >= 10.2f)
                    { command.Jump = true; SetPhase(Phase.SecondFlight, tick); }
                    break;
                case Phase.SecondFlight:
                    command = Along(direction, true);
                    if (self.Grounded && Mathf.Abs(position.y + 9.92f) < .2f) SetPhase(Phase.ReturnApproach, tick);
                    break;
                case Phase.ReturnApproach:
                    var pad = route.GetComponentInChildren<JumpPad>();
                    if (pad != null) command = Toward(position, pad.transform.position, false, true);
                    else
                    {
                        var gate = route.GetComponentInChildren<PortalGate>();
                        var approach = gate.transform.position + gate.transform.forward * 1.4f;
                        command = Toward(position, approach, false, true);
                        if (HorizontalDistance(position, approach) < .25f) SetPhase(Phase.PortalCross, tick);
                    }
                    break;
                case Phase.PortalCross:
                    var portal = route.GetComponentInChildren<PortalGate>();
                    command = Along(-portal.transform.forward, false);
                    if (tick - enteredTick > 180) SetPhase(Phase.ReturnApproach, tick);
                    break;
            }
            // Re-evaluate a grounded route after a blocked approach; never warp an actor to recover it.
            if (tick - enteredTick > 600 && self.Grounded) phase = Phase.Idle;
            return true;
        }
        void SetPhase(Phase next, int tick) { phase = next; enteredTick = tick; }
        static float HorizontalDistance(Vector3 a, Vector3 b) => new Vector2(a.x - b.x, a.z - b.z).magnitude;
        static MovementCommand Along(Vector3 direction, bool sprint)
            => new MovementCommand { Move = Vector2.up, Yaw = Quaternion.LookRotation(direction).eulerAngles.y, Sprint = sprint };
        static MovementCommand Toward(Vector3 position, Vector3 destination, bool sprint, bool walk)
        {
            var offset = destination - position; offset.y = 0;
            if (offset.sqrMagnitude < .015f) return default;
            var command = Along(offset, sprint); command.Walk = walk; return command;
        }
    }
}

