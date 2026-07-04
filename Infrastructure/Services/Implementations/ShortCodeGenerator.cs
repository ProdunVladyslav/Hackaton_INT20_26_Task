using System.Security.Cryptography;
using Infrastructure.Services.Interfaces;

namespace Infrastructure.Services.Implementations
{
    /// <summary>
    /// Produces 8-character short-link codes. Alphabet excludes visually
    /// ambiguous characters (0/O, 1/I/l) — these end up in URLs people read
    /// off ads and screenshots, not just click.
    /// </summary>
    public sealed class ShortCodeGenerator : IShortCodeGenerator
    {
        private const string Alphabet = "23456789abcdefghjkmnpqrstuvwxyz";
        private const int Length = 8;

        public string Generate()
        {
            Span<byte> bytes = stackalloc byte[Length];
            RandomNumberGenerator.Fill(bytes);

            Span<char> chars = stackalloc char[Length];
            for (var i = 0; i < Length; i++)
                chars[i] = Alphabet[bytes[i] % Alphabet.Length];

            return new string(chars);
        }
    }
}
