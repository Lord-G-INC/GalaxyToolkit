using GalaxyToolkit.Data;
using GalaxyToolkit.Symbols;
using System;
using System.IO;
using System.Text;

namespace GalaxyToolkit.Commands {
    public interface IToolkitCommand {
        string Keyword { get; }
        string HelpMessage { get; }

        CommandResult Execute(Toolkit toolkit, string[] args);
    }

    public class CommandResult {
        public static readonly CommandResult SilentSucess = new(true, "Command Executed Successfully");
        public static readonly CommandResult SilentFail = new(false);

        private readonly bool _success;
        private readonly string _message;

        public bool Success {
            get => _success;
        }

        public string Message {
            get => _message;
        }

        public CommandResult(bool success) {
            _success = success;
            _message = string.Empty;
        }

        public CommandResult(bool success, string message) {
            _success = success;
            _message = message;
        }
    }

    public class CrashCommand : IToolkitCommand {
        public string Keyword { get; } = "crash";
        public string HelpMessage { get; } = """
            CrashCommand usage: crash
              Crashes the game with a null OSReport message.
            """;

        public CommandResult Execute(Toolkit toolkit, string[] args) {
            toolkit.ToolMessage.Write(0xFFFFFFFF);
            return CommandResult.SilentSucess;
        }
    }

    public class FreezeCommand : IToolkitCommand {
        public const uint FalseCode = 0xFEu;
        public const uint TrueCode = (1u << 8) | 0xFEu;

        public string Keyword { get; } = "freeze";
        public string HelpMessage { get; } = """
            FreezeCommand usage: freeze <Value>
              Freezes/unfreezes the game.

              Value: A boolean (true/false).
            """;

        public CommandResult Execute(Toolkit toolkit, string[] args) {
            if (args.Length < 2)
                return this.ExitError();

            var freeze = Utils.ConvertToBool(args[1]);

            if (!freeze.HasValue)
                return this.ExitError();

            toolkit.ToolMessage.Write(freeze.Value ? TrueCode : FalseCode);
            return CommandResult.SilentSucess;
        }
    }

    public class LogCommand : IToolkitCommand {
        private static readonly string DumpDirectoryPath = Path.Combine(AppContext.BaseDirectory, "dump");

        public string Keyword { get; } = "log";
        public string HelpMessage { get; } = """
            LogCommand usage: log [Dump]
              Displays information about a crash.

              Dump: An optional boolean (true/false). If the log should be dumped to a file.
            """;

        public CommandResult Execute(Toolkit toolkit, string[] args) {
            var contextAddress = toolkit.GameMessage.Read<uint>();

            if (!Utils.IsValidAddress(contextAddress))
                return new(false, "Game message is not a valid OSContext pointer.");

            var sb = new StringBuilder();
            var grp = new BufferAddress(toolkit.Dolphin, contextAddress, 32).Read<uint>();
            var srro = new BufferAddress(toolkit.Dolphin, contextAddress + 0x198, 2).Read<uint>();

            var fpr = new BufferAddress(toolkit.Dolphin, contextAddress + 0x90, 32).Read<double>();
            var stackInfo = new BufferAddress(toolkit.Dolphin, grp[1], 2);

            WriteSplit(sb, "MAIN");
            sb.AppendLine($"""
                Context: {contextAddress:X8}  Region: {toolkit.Region}
                SRR0:    {srro[0]:X8}  SRR1: {srro[1]:X8}
                """);

            WriteSplit(sb, "GRP");
            for (var i = 0; i < 32; i++) {
                sb.Append($"R{i:D2}: {grp[i]:X8}  ");

                if ((i + 1) % 3 == 0)
                    sb.Append('\n');
            }
            sb.Append('\n');

            WriteSplit(sb, "SRR0MAP");
            sb.Append($"SRR0: {srro[0]:X8}  ");
            WriteSymbol(sb, srro[0]);

            WriteSplit(sb, "FRP");
            for (var i = 0; i < 32; i++) {
                var sign = fpr[i] > 0 ? "+" : fpr[i] < 0 ? "" : " ";
                sb.Append($"F{i:D2}: {sign}{fpr[i]:0.000E+00}  ");

                if ((i + 1) % 3 == 0)
                    sb.Append('\n');
            }
            sb.Append('\n');

            WriteSplit(sb, "TRACE");
            sb.AppendLine("Address:   BackChain  LR Save    Symbol");

            for (var i = 0; i < 16; i++) {
                var data = stackInfo.Read<uint>();
                sb.Append($"{stackInfo.EmulatedAddress:X8}:  {data[0]:X8}   {data[1]:X8}   ");
                WriteSymbol(sb, data[1]);

                if (!Utils.IsValidAddress(data[0]))
                    break;

                stackInfo = new(toolkit.Dolphin, data[0], 2);
            }

            WriteSplit(sb);

            var str = sb.ToString();

            if (args.Length > 1 && (Utils.ConvertToBool(args[1]) ?? false)) {
                Directory.CreateDirectory(DumpDirectoryPath);

                var path = Path.Combine(DumpDirectoryPath, $"crash-log-{DateTime.Now:dd-MM-yyyy-HH-mm-ss}.txt");
                File.WriteAllText(path, str);

                sb.AppendLine($"Dumped crash log to {path}.");
            }
            
            return new(true, sb.ToString());

            static void WriteSplit(StringBuilder sb, string? name = null) {
                sb.Append("------------------------------------------------- ");
                sb.AppendLine(name);
            }

            static void WriteSymbol(StringBuilder sb, uint address) {
                if (SymbolTable.TryLookup(address, out var result)) {
                    sb.Append(result.Symbol);
                    sb.Append(" + 0x");
                    sb.AppendLine((address - result.Address).ToString("X"));
                }
                else {
                    sb.AppendLine("???");
                }
            }
        }
    }

    public class ObjectCommand : IToolkitCommand {
        public string Keyword { get; } = "obj";
        public string HelpMessage { get; } = string.Empty;

        public CommandResult Execute(Toolkit toolkit, string[] args) {
            return CommandResult.SilentSucess;
        }
    }

    public class StageCommand : IToolkitCommand {
        public const byte Code = 3;

        public string Keyword { get; } = "stage";
        public string HelpMessage { get; } = """
            StageCommand usage: stage <StageName> <ScenarioNo> <StarNo>
              Sends the player to any stage.

              StageName: The internal name of a stage.
              ScenarioNo: The scenario number.
              StarNo: The star ID.
            """;

        public CommandResult Execute(Toolkit toolkit, string[] args) {
            if (args.Length < 4)
                return this.ExitError();

            toolkit.DataBuffer.Write(args[1]);

            if (sbyte.TryParse(args[2], out var scenarioNo)) {
                if (sbyte.TryParse(args[3], out var starNo)) {
                    toolkit.ToolMessage.Write((uint)((starNo << 16) | (scenarioNo << 8) | Code));
                    return CommandResult.SilentSucess;
                }

                return new(false, "Invalid StarNo.");
            }

            return new(false, "Invalid ScenarioID.");
        }
    }

    public class WarpCommand : IToolkitCommand {
        public const uint PosCode = 4u;
        public const uint GeneralPosCode = (1u << 8) | 4u;

        public string Keyword { get; } = "warp";
        public string HelpMessage { get; } = """
            WarpCommand usage: warp <Type> <Value>
              Warps the player to any position.

              Type: Either "Pos" or "GeneralPos".
              Value: Either a vector or a general position name.
            """;

        public CommandResult Execute(Toolkit toolkit, string[] args) {
            if (args.Length < 3)
                return this.ExitError();

            switch (args[1]) {
                case "Pos":
                    var strValues = args[2].Split(',', 3);

                    if (strValues.Length < 3)
                        return new(false, "Invalid value Vector3.");

                    for (uint i = 0; i < 3; i++) {
                        if (float.TryParse(strValues[i], out var value))
                            toolkit.DataBuffer.Write(value, i * sizeof(float));
                        else
                            return new(false, $"Unable to parse \"{strValues[i]}\" as a float.");
                    }

                    toolkit.ToolMessage.Write(PosCode);
                    return CommandResult.SilentSucess;
                case "GeneralPos":
                    toolkit.DataBuffer.Write(args[2]);
                    toolkit.ToolMessage.Write(GeneralPosCode);
                    return CommandResult.SilentSucess;
            }

            return new(false, $"Unknown warp type \"{args[1]}\".");
        }
    }

    public class ReadCommand : IToolkitCommand {
        public string Keyword { get; } = "read";
        public string HelpMessage { get; } = """
            CrashCommand usage: read <Type> <Address> [Format]
              Reads a value at a certain address.

              Address: The address to read the value from.
              Type: The value type. (Supported: s8, u8, s16, u16, s32, u32, s64, u64, f32, f64)
              Format: An optional value formatting.
            """;

        public CommandResult Execute(Toolkit toolkit, string[] args) {
            if (args.Length < 3)
                return this.ExitError();

            if (!uint.TryParse(Utils.RemoveHexSpecifier(args[2]), System.Globalization.NumberStyles.HexNumber, null, out var address))
                return new(false, $"Unable to parse address \"{args[2]}\".");

            try {
                var data = new Address(toolkit.Dolphin, address);

                var format = args.Length >= 4 ? args[3] : null;
                var result = args[1] switch {
                    "s8" => data.Read<sbyte>().ToString(format),
                    "u8" => data.Read<byte>().ToString(format),
                    "s16" => data.Read<short>().ToString(format),
                    "u16" => data.Read<ushort>().ToString(format),
                    "s32" => data.Read<int>().ToString(format),
                    "u32" => data.Read<uint>().ToString(format),
                    "s64" => data.Read<long>().ToString(format),
                    "u64" => data.Read<ulong>().ToString(format),
                    "f32" => data.Read<float>().ToString(format),
                    "f64" => data.Read<double>().ToString(format),
                    _ => throw new Exception($"Unknown data type \"{args[1]}\".")
                };

                return new(true, result);
            }
            catch (Exception ex) {
                return new(false, ex.Message);
            }
        }
    }

    public class WriteCommand : IToolkitCommand {
        public string Keyword { get; } = "write";
        public string HelpMessage { get; } = """
            CrashCommand usage: write <Type> <Address> <Value>
              Crashes the game with a null pointer exception.

              Address: The address to write the value to.
              Value: The value to write.
              Type: The value type. (Supported: s8, u8, s16, u16, s32, u32, s64, u64, f32, f64)
            """;

        public CommandResult Execute(Toolkit toolkit, string[] args) {
            if (args.Length < 4)
                return this.ExitError();

            if (!uint.TryParse(Utils.RemoveHexSpecifier(args[2]), System.Globalization.NumberStyles.HexNumber, null, out var address))
                return new(false, $"Unable to parse address \"{args[2]}\".");

            try {
                var data = new Address(toolkit.Dolphin, address);

                switch (args[1]) {
                    case "s8": data.Write(sbyte.Parse(args[3])); break;
                    case "u8": data.Write(byte.Parse(args[3])); break;
                    case "s16": data.Write(short.Parse(args[3])); break;
                    case "u16": data.Write(ushort.Parse(args[3])); break;
                    case "s32": data.Write(int.Parse(args[3])); break;
                    case "u32": data.Write(uint.Parse(args[3])); break;
                    case "s64": data.Write(long.Parse(args[3])); break;
                    case "u64": data.Write(ulong.Parse(args[3])); break;
                    case "f32": data.Write(float.Parse(args[3])); break;
                    case "f64": data.Write(double.Parse(args[3])); break;
                    default: throw new Exception($"Unknown data type \"{args[1]}\".");
                };

                return CommandResult.SilentSucess;
            }
            catch (Exception ex) {
                return new(false, ex.Message);
            }
        }
    }
}
