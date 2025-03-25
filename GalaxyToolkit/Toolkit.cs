using DolphinMemory;
using GalaxyToolkit.Commands;
using GalaxyToolkit.Data;
using GalaxyToolkit.Symbols;
using System;

/*
1 params -> (param1 << 8) | code
2 params -> (param2 << 16) | (param1 << 8) | code
3 params -> (param3 << 24) | (param2 << 16) | (param1 << 8) | code
*/

namespace GalaxyToolkit {
    public class Toolkit : IDisposable {
        public const uint RegionAddress = 0x80000003;
        public const uint ToolAccessAddress = 0x80002FF4;
        public const byte DataBufferSize = 64;

        private readonly Dolphin _dolphin;
        private readonly Address _toolMessage;
        private readonly Address _gameMessage;
        private readonly BufferAddress _dataBuffer;
        private readonly char _region;

        public Dolphin Dolphin {
            get => _dolphin;
        }

        public Address ToolMessage {
            get => _toolMessage;
        }

        public Address GameMessage {
            get => _gameMessage;
        }

        public BufferAddress DataBuffer {
            get => _dataBuffer;
        }

        public char Region {
            get => _region;
        }

        public Toolkit(Dolphin dolphin) {
            _dolphin = dolphin;

            try {
                _region = (char)new Address(_dolphin, RegionAddress).Read<byte>();
                SymbolTable.SetRegion(_region);
            }
            catch {
                throw new ToolkitException("Game is not currently running.");
            }

            var dataAddress = new Address(_dolphin, ToolAccessAddress).Read<uint>();

            if (dataAddress == 0)
                throw new ToolkitException("Data address is invalid. Wait for the Wiimote strap screen to end.");

            _toolMessage = new(_dolphin, dataAddress);
            _gameMessage = new(_dolphin, dataAddress + 4);
            _dataBuffer = new(_dolphin, dataAddress + 8, DataBufferSize);

            _toolMessage.Write(1);
            Utils.WriteLineColor($"Toolkit initialized, data located at 0x{dataAddress:X8}.", ConsoleColor.Blue);
        }

        public CommandResult ExecuteCommand(string input) {
            return CommandManager.ExecuteCommand(this, input);
        }

        public void Dispose() {
            _dolphin?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
