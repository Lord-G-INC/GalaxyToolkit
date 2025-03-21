using System;
using System.Linq;
using System.Runtime.InteropServices;

namespace GalaxyToolkit {
    public static class Utils {
        /// <summary>
        /// Reverses the endianness of an unmanaged object.
        /// </summary>
        /// <typeparam name="T">An unmanaged type.</typeparam>
        /// <param name="obj">An unmanaged object.</param>
        /// <returns>The value with reversed endianness.</returns>
        public static T ReverseEndian<T>(T obj) where T : unmanaged {
            MemoryMarshal.AsBytes(new Span<T>(ref obj)).Reverse();
            return obj;
        }

        /// <summary>
        /// Checks the specified address is a valid address the Wii's memory range.
        /// </summary>
        /// <param name="address">The address to check.</param>
        /// <returns>True if the address is in the valid range, otherwise false.</returns>
        public static bool IsValidAddress(uint address) {
            return 0x80000000 <= address && address <= 0x817FFFFF;
        }

        /// <summary>
        /// Ensures <paramref name="address"/> is a valid address the Wii's memory range.
        /// </summary>
        /// <param name="address">The address to check.</param>
        public static void EnsureValidAddress(uint address) {
            if (!IsValidAddress(address))
                throw new Exception($"Invalid address {address}");
        }

        /// <summary>
        /// Removes the hex specifier (0x) from a hexadecimal number string.
        /// </summary>
        /// <param name="s">The string to remove the specifier from.</param>
        /// <returns>The string without the hex specifier.</returns>
        public static string RemoveHexSpecifier(string s) {
            if (string.IsNullOrEmpty(s) || s.Length < 2)
                return s;

            if (s[0] == '0' && s[1] == 'x')
                return s[2..];

            return s;
        }

        /// <summary>
        /// Converts a <see cref="string"/> to a <see cref="bool?"/>.
        /// </summary>
        /// <param name="s">The string to convert.</param>
        /// <returns>The boolean, null if <paramref name="s"/> is an invalid bool.</returns>
        public static bool? ConvertToBool(string s) {
            if (s.Equals("true", StringComparison.OrdinalIgnoreCase) || s.Equals("1"))
                return true;

            if (s.Equals("false", StringComparison.OrdinalIgnoreCase) || s.Equals("0"))
                return false;

            return null;
        }

        /// <summary>
        /// Throws an exception if the region letter is invalid.
        /// </summary>
        /// <param name="letter">The region letter.</param>
        public static void EnsureValidRegion(char letter) {
            if (letter != 'E' && letter != 'P' && letter != 'J' && letter != 'K' && letter != 'W')
                throw new Exception($"Invalid region {letter}");
        }

        /// <summary>
        /// Gets the full name of a region based on its letter.
        /// </summary>
        /// <param name="letter">The region letter.</param>
        /// <returns>The region name, empty if <paramref name="letter"/> is an invalid region.</returns>
        public static string GetRegionName(char letter) {
            return letter switch {
                'E' => "USA",
                'P' => "PAL",
                'J' => "JPN",
                'K' => "KOR",
                'W' => "TWN",
                _ => string.Empty,
            };
        }

        public static void WriteColor(string? s, ConsoleColor color) {
            Console.ForegroundColor = color;
            Console.Write(s);
            Console.ResetColor();
        }

        public static void WriteLineColor(string? s, ConsoleColor color) {
            Console.ForegroundColor = color;
            Console.WriteLine(s);
            Console.ResetColor();
        }
    }
}
