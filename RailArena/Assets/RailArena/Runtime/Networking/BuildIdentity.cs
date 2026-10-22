using System;
using System.Globalization;
using UnityEngine;

namespace RailArena.Networking
{
    public static class BuildIdentity
    {
        public static string Fingerprint
        {
            get
            {
                var asset = Resources.Load<TextAsset>("BuildIdentity");
                string value = asset == null ? "" : asset.text.Trim();
                if (value.Length != 64 || !ulong.TryParse(value.Substring(0, 16), NumberStyles.HexNumber,
                    CultureInfo.InvariantCulture, out ulong token) || token == 0)
                    throw new InvalidOperationException("Build fingerprint missing. Generate it with Rail Arena/Generate build identity, or build the player.");
                return value;
            }
        }

        public static ulong Token => ulong.Parse(Fingerprint.Substring(0, 16), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
    }
}
