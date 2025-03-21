using DolphinMemory.Native;
using Reloaded.Memory;
using Reloaded.Memory.Interfaces;
using System;
using System.Diagnostics;

namespace DolphinMemory {
    public class Dolphin : IDisposable {
        private const long EmulatedMemorySize = 0x2000000;
        private const long EmulatedMemoryBase = 0x80000000;

        private nint _emulatedMemoryPointer;
        private readonly nint _baseAddress;
        private readonly int _moduleSize;

        private readonly ICanReadWriteMemory _memory;
        private readonly Process _process;

        public ICanReadWriteMemory Memory {
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
            _moduleSize = module.ModuleMemorySize;

            if (_process.Id == Environment.ProcessId)
                _memory = new Memory();
            else
                _memory = new ExternalMemory(_process);
        }

        public nuint GetAddress(long address) {
            if (TryGetBaseAddress(out var baseAddress))
                return (nuint)(address - EmulatedMemoryBase + baseAddress);

            return nuint.Zero;
        }

        public unsafe bool TryGetBaseAddress(out nint emulatedBaseAddress) {
            if (_emulatedMemoryPointer != nint.Zero) {
                _memory.Read((nuint)_emulatedMemoryPointer.ToInt64(), out emulatedBaseAddress);
                return emulatedBaseAddress != nint.Zero;
            }

            if (TryGetDolphinPage(out emulatedBaseAddress)) {
                var dolphinMainModule = new byte[_moduleSize];

                _memory.ReadRaw((nuint)_baseAddress.ToInt64(), dolphinMainModule);
                long readCount = _moduleSize - sizeof(nint);

                fixed (byte* mainModulePtr = dolphinMainModule) {
                    var lastAddress = (long)mainModulePtr + readCount;
                    var currentAddress = (long)mainModulePtr;

                    while (currentAddress < lastAddress) {
                        var current = *(nint*)currentAddress;

                        if (current == emulatedBaseAddress) {
                            var offset = currentAddress - (long)mainModulePtr;
                            _emulatedMemoryPointer = (nint)(_baseAddress + offset);
                            return true;
                        }

                        currentAddress += 1;
                    }
                }

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

        private unsafe bool IsDolphinPage(WinNative.BasicMemoryInformation memoryPage) {
            if (memoryPage.RegionSize == (nint)EmulatedMemorySize && memoryPage.lType == WinNative.PageType.Mapped) {
                var setInformation = new WinNative.PSApiWorkingSetExInfo[1];
                setInformation[0].VirtualAddress = memoryPage.BaseAddress;

                if (!WinNative.QueryWorkingSetEx(_process.Handle, setInformation, sizeof(WinNative.PSApiWorkingSetExInfo) * setInformation.Length))
                    return false;

                if (setInformation[0].VirtualAttributes.Valid)
                    return true;
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