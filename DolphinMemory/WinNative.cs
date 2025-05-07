using System;
using System.Runtime.InteropServices;
using System.Security;

namespace DolphinMemory {
    public static partial class WinNative {
        /// <summary>
        /// Retrieves information about a range of pages within the virtual address space of a specified process.
        /// </summary>
        /// <param name="hProcess">
        ///     A handle to the process whose memory information is queried.
        ///     The handle must have been opened with the PROCESS_QUERY_INFORMATION access right,
        ///     which enables using the handle to read information from the process object.
        /// </param>
        /// <param name="lpAddress">
        ///     A pointer to the base address of the region of pages to be queried.
        ///     This value is rounded down to the next page boundary.
        ///     To determine the size of a page on the host computer, use the GetSystemInfo function.
        /// </param>
        /// <param name="lpBuffer">A pointer to a MEMORY_BASIC_INFORMATION structure in which information about the specified page range is returned.</param>
        /// <param name="dwLength">The size of the buffer pointed to by the lpBuffer parameter, in bytes.</param>
        /// <returns></returns>

        [SuppressUnmanagedCodeSecurity]
        [LibraryImport("kernel32.dll", SetLastError = true)]
        public static partial int VirtualQueryEx(nint hProcess, nint lpAddress, out BasicMemoryInformation lpBuffer, uint dwLength);

        [SuppressUnmanagedCodeSecurity]
        [LibraryImport("psapi", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool QueryWorkingSetEx(nint hProcess, [In, Out] PSApiWorkingSetExInfo[] pv, int cb);

        /// <summary>
        /// <para>Retrieves information about the current system.</para>
        /// <para>To retrieve accurate information for an application running on WOW64, call the <c>GetNativeSystemInfo</c> function.</para>
        /// </summary>
        /// <param name="lpSystemInfo">A pointer to a <c>SYSTEM_INFO</c> structure that receives the information.</param>
        /// <returns>This function does not return a value.</returns>
        [SuppressUnmanagedCodeSecurity]
        [LibraryImport("kernel32.dll")]
        public static partial void GetSystemInfo(out SystemInfo lpSystemInfo);

        /// <summary>
        /// Contains information about the current computer system. This includes the architecture and type of the processor, the number of
        /// processors in the system, the page size, and other such information.
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 2)]
        public struct SystemInfo {
            /// <summary>
            /// The processor architecture of the installed operating system.
            /// </summary>
            public ProcessorArchitecture ProcessorArchitecture;

            /// <summary>
            /// This member is reserved for future use.
            /// </summary>
            public ushort Reserved;

            /// <summary>
            /// The page size and the granularity of page protection and commitment. This is the page size used by the <c>VirtualAlloc</c> function.
            /// </summary>
            public uint PageSize;

            /// <summary>
            /// A pointer to the lowest memory address accessible to applications and dynamic-link libraries (DLLs).
            /// </summary>
            public nint MinimumApplicationAddress;

            /// <summary>
            /// A pointer to the highest memory address accessible to applications and DLLs.
            /// </summary>
            public nint MaximumApplicationAddress;

            /// <summary>
            /// A mask representing the set of processors configured into the system. Bit 0 is processor 0; bit 31 is processor 31.
            /// </summary>
            public nuint ActiveProcessorMask;

            /// <summary>
            /// The number of logical processors in the current group. To retrieve this value, use the <c>GetLogicalProcessorInformation</c> function.
            /// </summary>
            public uint NumberOfProcessors;

            /// <summary>
            /// An obsolete member that is retained for compatibility. Use the <c>wProcessorArchitecture</c>, <c>wProcessorLevel</c>, and
            /// <c>wProcessorRevision</c> members to determine the type of processor.
            /// </summary>
            public uint ProcessorType;

            /// <summary>
            /// The granularity for the starting address at which virtual memory can be allocated. For more information, see <c>VirtualAlloc</c>.
            /// </summary>
            public uint AllocationGranularity;

            /// <summary>
            /// <para>
            /// The architecture-dependent processor level. It should be used only for display purposes. To determine the feature set of a
            /// processor, use the <c>IsProcessorFeaturePresent</c> function.
            /// </para>
            /// <para>If <c>wProcessorArchitecture</c> is PROCESSOR_ARCHITECTURE_INTEL, <c>wProcessorLevel</c> is defined by the CPU vendor.</para>
            /// <para>If <c>wProcessorArchitecture</c> is PROCESSOR_ARCHITECTURE_IA64, <c>wProcessorLevel</c> is set to 1.</para>
            /// </summary>
            public ushort ProcessorLevel;

            /// <summary>
            /// The architecture-dependent processor revision.
            /// </summary>
            public ushort ProcessorRevision;
        }

        /// <summary>
        /// Contains information about a range of pages in the virtual address space of a process.
        /// The VirtualQuery and VirtualQueryEx functions use this structure.
        /// </summary>
        public struct BasicMemoryInformation {
            /// <summary>
            /// A pointer to the base address of the region of pages.
            /// </summary>
            public nint BaseAddress;

            /// <summary>
            /// A pointer to the base address of a range of pages allocated by the VirtualAlloc function.
            /// The page pointed to by the BaseAddress member is contained within this allocation range.
            /// </summary>
            public nint AllocationBase;

            /// <summary>
            /// The memory protection option when the region was initially allocated.
            /// This member can be one of the memory protection constants or 0 if the caller does not have access.
            /// </summary>
            public MemoryProtections AllocationProtect;

            /// <summary>
            /// The size of the region beginning at the base address in which all pages have identical attributes, in bytes.
            /// </summary>
            public nint RegionSize;

            /// <summary>
            /// The state of the pages in the region.
            /// </summary>
            public PageState State;

            /// <summary>
            /// The access protection of the pages in the region.
            /// This member is one of the values listed for the AllocationProtect member.
            /// </summary>
            public MemoryProtections Protect;

            /// <summary>
            /// The type of pages in the region.
            /// </summary>
            public PageType Type;
        }

        /// <summary>
        /// MemoryProtections
        ///     Specifies the memory protection constants for the region of pages
        ///     to be allocated, referenced or used for a similar purpose.
        ///     https://msdn.microsoft.com/en-us/library/windows/desktop/aa366786(v=vs.85).aspx
        /// </summary>
        [Flags]
        public enum MemoryProtections {
            NoAccess = 1,
            ReadOnly = 2,
            ReadWrite = 4,
            WriteCopy = 8,
            Execute = 16, // 0x00000010
            ExecuteRead = 32, // 0x00000020
            ExecuteReadWrite = 64, // 0x00000040
            ExecuteWriteCopy = 128, // 0x00000080
            GuardModifierflag = 256, // 0x00000100
            NoCacheModifierflag = 512, // 0x00000200
            WriteCombineModifierflag = 1024, // 0x00000400
        }

        /// <summary>
        /// Processor architecture
        /// </summary>
        public enum ProcessorArchitecture : ushort {
            PROCESSOR_ARCHITECTURE_INTEL = 0,
            PROCESSOR_ARCHITECTURE_MIPS = 1,
            PROCESSOR_ARCHITECTURE_ALPHA = 2,
            PROCESSOR_ARCHITECTURE_PPC = 3,
            PROCESSOR_ARCHITECTURE_SHX = 4,
            PROCESSOR_ARCHITECTURE_ARM = 5,
            PROCESSOR_ARCHITECTURE_IA64 = 6,
            PROCESSOR_ARCHITECTURE_ALPHA64 = 7,
            PROCESSOR_ARCHITECTURE_MSIL = 8,
            PROCESSOR_ARCHITECTURE_AMD64 = 9,
            PROCESSOR_ARCHITECTURE_IA32_ON_WIN64 = 10,
            PROCESSOR_ARCHITECTURE_NEUTRAL = 11,
            PROCESSOR_ARCHITECTURE_ARM64 = 12,
            PROCESSOR_ARCHITECTURE_ARM32_ON_WIN64 = 13,
            PROCESSOR_ARCHITECTURE_UNKNOWN = 65535,
        }

        /// <summary>
        /// PageState
        ///     The state of the pages in the region. This member can be one of the following values.
        ///     https://msdn.microsoft.com/en-us/library/windows/desktop/aa366775(v=vs.85).aspx
        /// </summary>
        [Flags]
        public enum PageState {
            /// <summary>
            /// Indicates committed pages for which physical storage has been allocated, either in memory or in the paging file on disk.
            /// </summary>
            Commit = 4096, // 0x00001000
            /// <summary>
            /// Indicates reserved pages where a range of the process's virtual address space is reserved without any physical storage being allocated.
            /// For reserved pages, the information in the Protect member is undefined.
            /// </summary>
            Reserve = 8192, // 0x00002000
            /// <summary>
            /// Indicates free pages not accessible to the calling process and available to be allocated.
            /// For free pages, the information in the AllocationBase, AllocationProtect, Protect, and Type members is undefined.
            /// </summary>
            Free = 65536, // 0x00010000
        }

        /// <summary>
        /// PageState
        ///     The type of pages in the region. The following types are defined.
        ///     https://msdn.microsoft.com/en-us/library/windows/desktop/aa366775(v=vs.85).aspx
        /// </summary>
        [Flags]
        public enum PageType {
            /// <summary>
            /// Indicates that the memory pages within the region are mapped into the view of an image section.
            /// </summary>
            Image = 16777216, // 0x01000000

            /// <summary>
            /// Indicates that the memory pages within the region are private (that is, not shared by other processes).
            /// </summary>
            Private = 131072, // 0x00020000

            /// <summary>
            /// Indicates that the memory pages within the region are mapped into the view of a section.
            /// </summary>
            Mapped = 262144, // 0x00040000
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct PSApiWorkingSetExInfo {
            public nint VirtualAddress;
            public PSApiWorkingSetExBlock VirtualAttributes;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct PSApiWorkingSetExBlock {
            /// <summary>
            /// The working set information.
            /// </summary>
            public nuint Flags;

            /// <summary>
            /// If <see langword="true"/>, the page is valid; otherwise, the page is not valid.
            /// </summary>
            public readonly bool Valid => (Flags & 1) == 1;
        }
    }
}