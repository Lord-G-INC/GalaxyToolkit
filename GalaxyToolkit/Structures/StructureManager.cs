using System;
using System.Collections.Generic;
using System.IO;

namespace GalaxyToolkit.Structures {
    using StructureList = Dictionary<string, GalaxyStructure>;

    public static class StructureManager {
        private static readonly string StructuresPath = Path.Combine(AppContext.BaseDirectory, "Structures");
        private static readonly StructureList _structures = [];

        public static StructureList Structures {
            get => _structures;
        }

        public static void RegisterStructure<T>(T structure) where T : GalaxyStructure {
            _structures.Add(structure.Name, structure);
        }

        public static void RegisterAllStructures() {
            foreach (var file in Directory.EnumerateFiles(StructuresPath, "*.json", SearchOption.TopDirectoryOnly)) {
                try {
                    var gs = new GalaxyStructure(file);
                    RegisterStructure(gs);

                    Console.WriteLine(gs);
                }
                catch (Exception ex) {
                    Utils.WriteLineColor($"Error loading galaxy structure from {file}: {ex.Message}", ConsoleColor.Red);
                }
            }
        }
    }
}
