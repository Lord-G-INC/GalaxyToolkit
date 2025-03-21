using DolphinMemory.Native;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace DolphinMemory {
    public sealed class MemoryPageEnumerator : IEnumerator<WinNative.BasicMemoryInformation> {
        private readonly Process _process;
        private readonly ulong _maxAddress = 0x7FFFFFFF;

        private WinNative.BasicMemoryInformation _current;
        private ulong _currentAddress = 0;

        public WinNative.BasicMemoryInformation Current => _current;
        object IEnumerator.Current => _current;

        public MemoryPageEnumerator(Process process) {
            _process = process;

            WinNative.GetSystemInfo(out var systemInfo);
            _maxAddress = (ulong)systemInfo.lpMaximumApplicationAddress;
        }

        public unsafe bool MoveNext() {
            if (_currentAddress > _maxAddress)
                return false;

            _ = WinNative.VirtualQueryEx(_process.Handle, (nint)_currentAddress, out _current, (uint)sizeof(WinNative.BasicMemoryInformation));
            _currentAddress += (ulong)_current.RegionSize;

            return true;
        }

        public void Reset() => _currentAddress = 0;

        public void Dispose() { }
    }
}
