using DolphinMemory;

namespace GalaxyToolkit.Data {
    /// <summary>
    /// Wraps an emulated address that points to an unmanaged value and provides methods to interact with its data.
    /// </summary>
    /// <typeparam name="TData">The type of value stored at this address.</typeparam>
    public readonly struct Address {
        private readonly Dolphin _dolphin;
        private readonly uint _emuAddress;

        /// <summary>
        /// The emulated address.
        /// </summary>
        public uint EmulatedAddress {
            get => _emuAddress;
        }

        /// <summary>
        /// The physical address.
        /// </summary>
        public nuint RealAddress {
            get => _dolphin.GetAddress(_emuAddress);
        }

        public Address(Dolphin dolphin, uint address) {
            _dolphin = dolphin;
            _emuAddress = address;
        }

        /// <summary>
        /// Reads the value at this address.
        /// </summary>
        /// <returns>The read value.</returns>
        public readonly TData Read<TData>() where TData : unmanaged {
            _dolphin.EnsureProcessOpen();
            return Utils.ReverseEndian(_dolphin.Memory.Read<TData>(RealAddress));
        }

        /// <summary>
        /// Writes data to the address.
        /// </summary>
        /// <param name="value">The value to write.</param>
        public readonly void Write<TData>(TData value) where TData : unmanaged {
            _dolphin.EnsureProcessOpen();
            _dolphin.Memory.Write(RealAddress, Utils.ReverseEndian(value));
        }

        public override string ToString() {
            return _emuAddress.ToString("X8");
        }
    }
}