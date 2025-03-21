using DolphinMemory;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Text;

namespace GalaxyToolkit.Data {
    /// <summary>
    /// Wraps an emulated address that points to a byte span and provides methods to interact with its data.
    /// </summary>
    public readonly struct BufferAddress {
        private readonly Dolphin _dolphin;
        private readonly uint _emuAddress;
        private readonly uint _size;

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

        /// <summary>
        /// The size of this buffer.
        /// </summary>
        public uint Size {
            get => _size;
        }

        public BufferAddress(Dolphin dolphin, uint address, uint size) {
            _dolphin = dolphin;
            _emuAddress = address;
            _size = size;
        }

        /// <summary>
        /// Reads a value from this buffer at a specific offset.
        /// </summary>
        /// <typeparam name="T">The type to read.</typeparam>
        /// <param name="offset">The offset of this value.</param>
        /// <returns>The read value.</returns>
        public unsafe T Read<T>(uint offset) where T : unmanaged {
            _dolphin.EnsureProcessOpen();

            if (offset + sizeof(T) > _size * sizeof(T))
                ThrowReadOverflow();

            return Utils.ReverseEndian(_dolphin.Memory.Read<T>(RealAddress + offset));
        }

        /// <inheritdoc cref="Read(uint, uint)"/>
        public byte[] Read() => Read(0, _size);

        /// <summary>
        /// Reads a span of bytes from this buffer.
        /// </summary>
        /// <param name="offset">The offset to read the span from.</param>
        /// <param name="length">The amount of bytes to read.</param>
        /// <returns>The read bytes.</returns>
        public byte[] Read(uint offset, uint length) {
            _dolphin.EnsureProcessOpen();

            if (offset + length > _size)
                ThrowReadOverflow();

            var arr = new byte[length];
            _dolphin.Memory.ReadRaw(RealAddress + offset, arr);

            return arr;
        }

        /// <inheritdoc cref="Read{T}(uint, uint)"/>
        public T[] Read<T>() where T : unmanaged => Read<T>(0, _size);

        /// <summary>
        /// Reads an array of <typeparamref name="T"/> from this buffer.
        /// </summary>
        /// <typeparam name="T">The span element type.</typeparam>
        /// <param name="offset">The offset to read the span from.</param>
        /// <param name="length">The amount of bytes to read.</param>
        /// <returns>The read values.</returns>
        public unsafe T[] Read<T>(uint offset, uint length) where T : unmanaged {
            _dolphin.EnsureProcessOpen();

            if (offset + length * sizeof(T) > _size * sizeof(T))
                ThrowReadOverflow();

            var arr = new T[length];
            _dolphin.Memory.ReadRaw(RealAddress + offset, MemoryMarshal.AsBytes<T>(arr));

            for (var i = 0; i < length; i++)
                arr[i] = Utils.ReverseEndian(arr[i]);

            return arr;
        }

        /// <summary>
        /// Writes a value to this buffer.
        /// </summary>
        /// <typeparam name="TData">The type to write.</typeparam>
        /// <param name="value">The data to write.</param>
        /// <param name="offset">The offset to read from.</param>
        public unsafe void Write<TData>(TData value, uint offset) where TData : unmanaged {
            _dolphin.EnsureProcessOpen();

            if (offset + sizeof(TData) > _size)
                ThrowWriteOverflow();

            _dolphin.Memory.Write(RealAddress + offset, Utils.ReverseEndian(value));
        }

        /// <summary>
        /// Writes raw data to this buffer.
        /// </summary>
        /// <param name="data">The data to write.</param>
        public void Write(Span<byte> data) {
            _dolphin.EnsureProcessOpen();

            if (data.Length > _size)
                ThrowWriteOverflow();

            WriteCore(data);
        }

        /// <summary>
        /// Writes a null-terminated string to this buffer.
        /// </summary>
        /// <param name="s">The string to write.</param>
        /// <param name="enc">The encoding. Set to <see cref="Encoding.ASCII"/> by default.</param>
        public void Write(string s, Encoding? enc = null) {
            enc ??= Encoding.ASCII;
            var data = enc.GetBytes(s + '\0');

            var charSize = data.Length / s.Length;

            if (data.Length + charSize > _size)
                ThrowWriteOverflow();

            WriteCore(data);
        }

        private void WriteCore(Span<byte> data) {
            _dolphin.Memory.WriteRaw(RealAddress, data);
        }

        [DoesNotReturn]
        private void ThrowReadOverflow() {
            throw new OverflowException($"Attempted to read outside of this buffer, must be under {_size} bytes long.");
        }

        [DoesNotReturn]
        private void ThrowWriteOverflow() {
            throw new OverflowException($"Attempted to write outside of this buffer, must be under {_size} bytes long.");
        }

        public override string ToString() {
            return _emuAddress.ToString("X8");
        }
    }
}
