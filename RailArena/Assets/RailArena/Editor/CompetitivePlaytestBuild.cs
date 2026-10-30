using System;
using System.IO;
using UnityEngine;

namespace RailArena.Editor
{
    /// <summary>Explicit new output paths keep the supplied baseline player intact.</summary>
    public static class CompetitivePlaytestBuild
    {
        public static void Build()
        {
            string[] args = Environment.GetCommandLineArgs();
            int option = Array.IndexOf(args, "--rail-playtest-output");
            if (option < 0 || option + 1 >= args.Length) throw new ArgumentException("Supply --rail-playtest-output with a new output folder.");
            string output = Path.GetFullPath(args[option + 1]);
            if (Directory.Exists(output)) throw new IOException("Playtest output already exists: " + output);
            Directory.CreateDirectory(output);
            ArenaBuilder.BuildWindowsPlaytestTo(Path.Combine(output, "RailArena.exe"));
            Debug.Log("COMPETITIVE_PLAYTEST_BUILT " + output);
        }
    }
}
