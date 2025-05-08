using DolphinMemory;
using GalaxyToolkit.Symbols;
using Reloaded.Memory;
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace GalaxyToolkit {
    internal class Program {
        static void Main(string[] args) {
            while (true) {
                Console.WriteLine("""
                    ------------ GalaxyToolkit ------------
                    [C] Connect to Dolphin
                    [Q] Quit
                    """);

                var key = Console.ReadKey(true);
                Console.WriteLine();

                switch (key.Key) {
                    case ConsoleKey.C:
                        Run();
                        break;
                    case ConsoleKey.Q:
                        return;
                }
            }
        }

        static void Run() {
            if (!TryConnectDolphin(out var dolpin))
                return;

            try {
                var toolkit = new Toolkit(dolpin);

                if (TryLoadBaseSymbols()) {
                    if (TryLoadSyatiSymbols()) {
                        Utils.WriteLineColor($"All symbols loaded. Syati address range: 0x{SymbolTable.SyatiStartAddress:X8} - 0x{SymbolTable.SyatiEndAddress:X8}", ConsoleColor.Blue);
                    }
                    else {
                        Utils.WriteLineColor("Base game symbols loaded.", ConsoleColor.Blue);
                    }
                }

                while (true) {
                    Console.Write("> ");
                    var command = Console.ReadLine();

                    if (!string.IsNullOrEmpty(command)) {
                        if (command == "quit") {
                            Console.WriteLine();
                            break;
                        }

                        toolkit.ExecuteCommand(command);
                    }
                }
            }
            catch (ToolkitException ex) {
                Utils.WriteLineColor(ex.Message, ConsoleColor.Red);
            }

            SymbolTable.UnloadSymbols();
        }

        static bool TryConnectDolphin([NotNullWhen(true)] out Dolphin? dolphin) {
            var processes = Process.GetProcessesByName(OperatingSystem.IsWindows() ? "dolphin" : "dolphin-emu");
            var idx = 0;

            if (processes.Length == 0) {
                Utils.WriteLineColor("Dolphin process not found.", ConsoleColor.Red);
                goto EndError;
            }
            else if (processes.Length > 1) {
                Console.WriteLine("Multiple dolphin processes found, select one from the following list:");

                for (var i = 0; i < processes.Length; i++)
                    Console.WriteLine($"[{i}] {processes[i].MainWindowTitle}");

                var s = Console.ReadLine();

                if (!int.TryParse(s, out idx) || idx < 0 || idx >= processes.Length) {
                    Utils.WriteLineColor("Specified index is invalid.", ConsoleColor.Red);
                    goto EndError;
                }
            }

            try {
                dolphin = new Dolphin(processes[idx]);
                return true;
            }
            catch (Exception ex) {
                Utils.WriteLineColor($"Dolphin connection error: {ex.Message}", ConsoleColor.Red);
                goto EndError;
            }

        EndError:
            dolphin = null;
            return false;
        }

        static bool TryLoadBaseSymbols() {
            try {
                SymbolTable.LoadBaseSymbols().GetAwaiter().GetResult();
                return true;
            }
            catch (Exception ex) {
                Utils.WriteLineColor($"Base symbols error: {ex.Message}", ConsoleColor.Red);
                return false;
            }
        }

        static bool TryLoadSyatiSymbols() {
            try {
                Utils.WriteColor("Enter a custom code map file (Optional): ", ConsoleColor.DarkGray);
                var path = Console.ReadLine();

                if (File.Exists(path)) {
                    Utils.WriteColor("Enter the Syati patch address (Found in Dolphin's log): ", ConsoleColor.DarkGray);
                    var str = Console.ReadLine();

                    if (string.IsNullOrEmpty(str))
                        throw new Exception($"No address was inputed.");

                    var address = uint.Parse(Utils.RemoveHexSpecifier(str), System.Globalization.NumberStyles.HexNumber);

                    SymbolTable.SetSyatiRegionStart(address);
                    SymbolTable.LoadSyatiSymbols(path);

                    return true;
                }
                else {
                    SymbolTable.SetSyatiRegionDefault();

                    if (!string.IsNullOrEmpty(path))
                        Utils.WriteLineColor($"Syati symbols were not found.", ConsoleColor.Red);

                    return false;
                }
            }
            catch (Exception ex) {
                Utils.WriteLineColor($"Syati symbols error: {ex.Message}", ConsoleColor.Red);
                return false;
            }
        }
    }
}
