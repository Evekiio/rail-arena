using System;
using RailArena.Simulation;
using UnityEngine;
namespace RailArena
{
    public partial class ArenaSession
    {
        private readonly int[] presentedPortal = new int[CombatRules.MaximumContenders];
        private readonly int[] presentedPad = new int[CombatRules.MaximumContenders];
        private void ResetTraversalPresentation()
        {
            Array.Clear(presentedPortal, 0, presentedPortal.Length); Array.Clear(presentedPad, 0, presentedPad.Length);
            foreach (var gate in World.Portals) gate.Presentation?.ResetPulse();
            foreach (var pad in World.Pads) pad.Presentation?.ResetPulse();
        }
        private void PresentPortal(int actorId, int serial, PortalGate exit)
        {
            if (serial <= presentedPortal[actorId]) return;
            presentedPortal[actorId] = serial;
            exit.Presentation?.Activate(true); exit.Exit?.Presentation?.Activate();
        }
        private void PresentPad(int actorId, int readyTick, JumpPad pad)
        {
            if (readyTick <= presentedPad[actorId]) return;
            presentedPad[actorId] = readyTick; pad.Presentation?.Activate();
        }
        private void PresentAuthorityTraversal(int actorId, int serial, int readyTick, Vector3 position, int tick, bool reset)
        {
            if (reset) { presentedPortal[actorId] = serial; presentedPad[actorId] = readyTick; return; }
            if (serial > presentedPortal[actorId])
            {
                foreach (var gate in World.Portals)
                    if (Vector3.Distance(position, gate.transform.position) < 5) { PresentPortal(actorId, serial, gate); break; }
                presentedPortal[actorId] = serial;
            }
            if (readyTick > presentedPad[actorId])
            {
                if (tick >= readyTick - 90 && tick - (readyTick - 90) <= 12)
                    foreach (var pad in World.Pads)
                        if (Vector3.Distance(position, pad.transform.position) < 8) { PresentPad(actorId, readyTick, pad); break; }
                presentedPad[actorId] = readyTick;
            }
        }
    }
}
