using System.Collections.Generic;

namespace RailArena
{
    public sealed class CombatFeed
    {
        public const int Capacity = 3;
        public const double Lifetime = 6;
        public readonly struct Entry
        {
            public readonly string Text;
            public readonly double ExpiresAt;
            public Entry(string text,double expiresAt) {Text=text;ExpiresAt=expiresAt;}
        }
        private readonly List<Entry> entries = new(Capacity);
        public IReadOnlyList<Entry> Entries => entries;
        public void Add(string text,double now)
        {
            if(string.IsNullOrWhiteSpace(text))return;
            Prune(now);
            if(entries.Count==Capacity)entries.RemoveAt(0);
            entries.Add(new Entry(text,now+Lifetime));
        }
        public void Prune(double now)
        { while(entries.Count>0 && entries[0].ExpiresAt<=now)entries.RemoveAt(0); }
        public void Clear() => entries.Clear();
    }
}
