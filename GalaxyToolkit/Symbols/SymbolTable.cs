using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace GalaxyToolkit.Symbols {
    public static class SymbolTable {
        private static char _region;
        private static uint _syatiStartAddress;
        private static uint _syatiEndAddress;

        private static FrozenDictionary<uint, string>? _baseSymbols;
        private static FrozenDictionary<uint, string>? _syatiSymbols;

        public static char Region {
            get => _region;
        }

        public static uint SyatiStartAddress {
            get => _syatiStartAddress;
        }

        public static uint SyatiEndAddress {
            get => _syatiEndAddress;
        }

        public static void SetRegion(char region) {
            Utils.EnsureValidRegion(region);
            _region = region;
        }

        public static void SetSyatiRegionStart(uint address) {
            Utils.EnsureValidAddress(address);
            _syatiStartAddress = address;
        }

        public static void SetSyatiRegionDefault() {
            _syatiStartAddress = _region switch {
                'J' => 0x807F2948,
                'E' => 0x807F3188,
                'P' => 0x807F8888,
                'K' => 0x807F2010,
                'W' => 0x807F3450,
                _ => 0,
            };

            _syatiEndAddress = 0x817FFFFF;
        }

        public static async Task LoadBaseSymbols() {
            using var ms = new MemoryStream();
            using var reader = new StreamReader(ms);

            var url = $"https://raw.githubusercontent.com/SMGCommunity/Syati/refs/heads/main/symbols/{Utils.GetRegionName(_region)}.txt";
            using (var client = new HttpClient())
            using (var data = await client.GetStreamAsync(url))
                await data.CopyToAsync(ms);

            ms.Position = 0;

            var symbols = new Dictionary<uint, string>();
            var line = reader.ReadLine();

            while (line != null) {
                var parts = line.Split('=', 2);

                if (parts.Length < 2)
                    goto NextLine;

                var address = uint.Parse(Utils.RemoveHexSpecifier(parts[1]), NumberStyles.HexNumber);
                symbols[address] = parts[0];

                NextLine:
                line = reader.ReadLine();
            }

            _baseSymbols = symbols.ToFrozenDictionary();
        }

        public static void LoadSyatiSymbols(string path) {
            var symbols = new Dictionary<uint, string>();

            using var source = File.OpenRead(path);
            using var reader = new StreamReader(source);

            for (var i = 0; i < 2; i++)
                while (reader.Read() != '\n') { }

            var line = reader.ReadLine();
            var address = 0u;
            var size = 0u;

            while (line != null) {
                var parts = line[2..].Split(' ', 3);

                if (parts.Length != 3)
                    goto NextLine;

                address = uint.Parse(parts[0], NumberStyles.HexNumber) + _syatiStartAddress;
                size = uint.Parse(parts[1], NumberStyles.HexNumber);

                symbols[address] = parts[2];

            NextLine:
                line = reader.ReadLine();
            }

            _syatiSymbols = symbols.ToFrozenDictionary();
            _syatiEndAddress = address + size + 4;
        }

        public static void UnloadSymbols() {
            _baseSymbols = null;
            _syatiSymbols = null;
        }

        public static LookupResult Lookup(uint address) {
            if (_baseSymbols is null)
                throw new Exception("Base symbols are not loaded.");

            Utils.EnsureValidAddress(address);

            if (address < _syatiStartAddress)
                return LookupCore(address, 0x80000000, _baseSymbols);

            if (_syatiSymbols is null)
                throw new Exception("Custom code symbols are not loaded.");

            if (address > _syatiEndAddress)
                throw new Exception("Custom code symbol is out of range.");

            return LookupCore(address, _syatiStartAddress, _syatiSymbols);
        }

        private static LookupResult LookupCore(uint address, uint baseAddress, FrozenDictionary<uint, string> symbols) {
            while (address >= baseAddress) {
                if (symbols.TryGetValue(address, out var symbol))
                    return new LookupResult() {
                        Symbol = symbol,
                        Address = address
                    };

                address--;
            }

            return default;
        }

        public static bool TryLookup(uint address, [NotNullWhen(true)] out LookupResult result) {
            try {
                result = Lookup(address);
                return true;
            }
            catch {
                result = default;
                return false;
            }
        }

        public struct LookupResult {
            public string Symbol;
            public uint Address;
        }
    }
}
