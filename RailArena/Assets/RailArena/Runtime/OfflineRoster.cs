using System;
using UnityEngine;

namespace RailArena
{
    public static class OfflineRoster
    {
        public const int CharacterCount = 4;
        private static readonly string[] Labels = { "PLAYER / IVORY", "BOT 01 / EMBER", "BOT 02 / TEAL", "BOT 03 / PLUM" };
        private static readonly string[] Colors = { "IVORY", "EMBER", "TEAL", "PLUM" };

        public static int NormalizeCharacter(int character) => Mathf.Clamp(character,0,CharacterCount-1);
        public static string Label(int character) => Labels[NormalizeCharacter(character)];
        public static string BotName(int character) => "BOT // " + Colors[NormalizeCharacter(character)];
        public static string PrefabPath(int character)
        {
            int index = NormalizeCharacter(character);
            return index == 0 ? "Characters/Contender" : $"Characters/Stylized/Bot{index:00}";
        }

        public static int CharacterForActor(int selected, int actorIndex)
        {
            if (actorIndex < 0 || actorIndex >= CharacterCount) throw new ArgumentOutOfRangeException(nameof(actorIndex));
            return (NormalizeCharacter(selected) + actorIndex) % CharacterCount;
        }
    }
}
