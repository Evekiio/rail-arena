using System.Collections.Generic;
using UnityEngine;

namespace RailArena
{
    public sealed class ArenaWorld : MonoBehaviour
    {
        public bool CarefulNavigation;
        public float RouteArrivalRadius = 1.4f;
        public int[] CarefulDropNodes = System.Array.Empty<int>();
        public string LayoutName = "FOUNDRY";
        public float MenuFieldOfView;
        public Vector3 MenuEye = new(39, 28, -43);
        public Vector3 MenuLook = new(25, -40, 0);
        public PortalGate[] Portals = System.Array.Empty<PortalGate>();
        public Transform[] Spawns;
        public Transform[] Nodes;
        public Vector2Int[] Links;
        public JumpPad[] Pads;
        public HealthPickup[] Pickups;

        public int NearestNode(Vector3 point)
        {
            int best = 0;
            float distance = float.MaxValue;
            for (int i = 0; i < Nodes.Length; i++)
            {
                float candidate = (Nodes[i].position - point).sqrMagnitude;
                if (candidate < distance) { distance = candidate; best = i; }
            }
            return best;
        }

        public int NextNode(int start, int goal)
        {
            var costs = new float[Nodes.Length];
            var previous = new int[Nodes.Length];
            var closed = new bool[Nodes.Length];
            for (int i = 0; i < costs.Length; i++) { costs[i] = float.MaxValue; previous[i] = -1; }
            costs[start] = 0;
            for (int step = 0; step < Nodes.Length; step++)
            {
                int current = -1;
                for (int i = 0; i < Nodes.Length; i++)
                    if (!closed[i] && (current < 0 || costs[i] < costs[current])) current = i;
                if (current < 0 || costs[current] == float.MaxValue) break;
                if (current == goal) break;
                closed[current] = true;
                foreach (var link in Links)
                {
                    int next = link.x == current ? link.y : -1;
                    if (next < 0 || closed[next]) continue;
                    float cost = costs[current] + Vector3.Distance(Nodes[current].position, Nodes[next].position);
                    if (cost < costs[next]) { costs[next] = cost; previous[next] = current; }
                }
            }
            int result = goal;
            while (previous[result] >= 0 && previous[result] != start) result = previous[result];
            return previous[result] < 0 ? start : result;
        }
    }

}
