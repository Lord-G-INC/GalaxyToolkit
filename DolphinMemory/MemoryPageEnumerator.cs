using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;

namespace DolphinMemory {
    public sealed partial class MemoryPageEnumerator : IEnumerator<MemoryInformation> {
        private readonly Process _process;
        private readonly ulong _maxAddress = 0x7FFFFFFF;

        private MemoryInformation _current;
        private ulong _currentAddress = 0;
        private int _iterationCount = 0;
        private bool _isDone = false;

        public MemoryInformation Current => _current;
        object IEnumerator.Current => _current;

        public MemoryPageEnumerator(Process process) {
            _process = process;

            if (OperatingSystem.IsWindows()) {
                WinNative.GetSystemInfo(out var systemInfo);
                _maxAddress = (ulong)systemInfo.MaximumApplicationAddress;
            }
            else {
                _maxAddress = 0x00007FFFFFFEFFFF; // Not checked
            }
        }

        public unsafe bool MoveNext() {
            if (_currentAddress > _maxAddress || _isDone)
                return false;

            if (OperatingSystem.IsWindows()) {
                _ = WinNative.VirtualQueryEx(_process.Handle, (nint)_currentAddress, out var winCurrent, (uint)sizeof(WinNative.BasicMemoryInformation));
                _current = new MemoryInformation() {
                    BaseAddress = winCurrent.BaseAddress,
                    RegionSize = winCurrent.RegionSize,
                    Type = winCurrent.Type
                };
                _iterationCount++;
            }
            else {
                using var reader = new StreamReader($"proc/{_process.Id}/maps");

                for (int i = 0; i < ++_iterationCount; i++)
                    reader.ReadLine();

                var line = reader.ReadLine();

                if (line is null) {
                    _isDone = true;
                    return false;
                }

                var rx = LinuxMapRegex().Match(line);
                var baseAddress = nint.Parse(rx.Groups["Begin"].Value, System.Globalization.NumberStyles.HexNumber);
                var regionSize = nint.Parse(rx.Groups["End"].Value, System.Globalization.NumberStyles.HexNumber) - baseAddress;

                _current = new MemoryInformation() {
                    BaseAddress = baseAddress,
                    RegionSize = regionSize,
                    Type = WinNative.PageType.Mapped
                };
            }

            _currentAddress += (ulong)_current.RegionSize;
            return true;
        }

        public void Reset() {
            _currentAddress = 0;
            _iterationCount = 0;
            _isDone = false;
        }

        public void Dispose() { }

        [GeneratedRegex(@"^(?<Begin>[0-9a-zA-Z]+)-(?<End>[0-9a-zA-Z]+)")]
        private static partial Regex LinuxMapRegex();
    }

    public readonly struct MemoryInformation {
        /// <summary>
        /// A pointer to the base address of the region of pages.
        /// </summary>
        public nint BaseAddress { get; init; }

        /// <summary>
        /// The size of the region, in bytes.
        /// </summary>
        public nint RegionSize { get; init; }

        /// <summary>
        /// The type of pages in the region.
        /// Always <see cref="WinNative.PageType.Mapped"/> in Linux.
        /// </summary>
        public WinNative.PageType Type { get; init; }
    }
}
