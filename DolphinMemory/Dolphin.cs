using Reloaded.Memory;
using System;
using System.Diagnostics;

#pragma warning disable CA1416

namespace DolphinMemory {
    public class Dolphin : IDisposable {
        private const long EmulatedMemorySize = 0x2000000;
        private const long EmulatedMemoryBase = 0x80000000;

        private readonly ExternalMemory _memory;
        private readonly Process _process;
        private readonly nint _baseAddress;

        public ExternalMemory Memory {
            get => _memory;
        }

        public Process Process {
            get => _process;
        }

        public Dolphin(Process process) {
            _process = process;
            var module = _process.MainModule;

            if (module is null)
                throw new Exception("Process address information could not be read.");

            _baseAddress = module.BaseAddress;
            _memory = new ExternalMemory(_process);
        }

        public nuint GetAddress(long address) {
            if (TryGetBaseAddress(out var baseAddress))
                return (nuint)(address - EmulatedMemoryBase + baseAddress);

            return nuint.Zero;
        }

        public unsafe bool TryGetBaseAddress(out nint emulatedBaseAddress) {
            if (TryGetDolphinPage(out emulatedBaseAddress)) {
                return true;
            }

            emulatedBaseAddress = nint.Zero;
            return false;
        }

        private bool TryGetDolphinPage(out nint baseAddress) {
            var enumerator = new MemoryPageEnumerator(_process);

            while (enumerator.MoveNext()) {
                var page = enumerator.Current;

                if (IsDolphinPage(page)) {
                    baseAddress = page.BaseAddress;
                    return true;
                }
            }

            baseAddress = nint.Zero;
            return false;
        }

        private unsafe bool IsDolphinPage(MemoryInformation memory) {
            if (memory.RegionSize == (nint)EmulatedMemorySize && memory.Type == WinNative.PageType.Mapped) {
                if (OperatingSystem.IsWindows()) {
                    var setInformation = new WinNative.PSApiWorkingSetExInfo[1];
                    setInformation[0].VirtualAddress = memory.BaseAddress;

                    if (!WinNative.QueryWorkingSetEx(_process.Handle, setInformation, sizeof(WinNative.PSApiWorkingSetExInfo) * setInformation.Length))
                        return false;

                    return setInformation[0].VirtualAttributes.Valid;
                }
                else return true;
            }

            return false;
        }

        public void EnsureProcessOpen() {
            if (_process.HasExited)
                throw new Exception("Dolphin process is closed.");
        }

        public void Dispose() {
            _process?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}