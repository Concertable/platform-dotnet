using System.Buffers.Binary;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Vogen;

namespace Concertable.Kernel.ValueObjects;

[ValueObject<string>(throws: typeof(DomainException),
    conversions: Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public sealed partial record Sha256Digest
{
    public const int Length = 64;

    public static Sha256Digest Of(params ReadOnlySpan<object?> parts)
    {
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        Span<byte> length = stackalloc byte[sizeof(int)];
        foreach (var part in parts)
        {
            if (part is null)
            {
                BinaryPrimitives.WriteInt32BigEndian(length, -1);
                hash.AppendData(length);
                continue;
            }

            var bytes = Encoding.UTF8.GetBytes(part switch
            {
                DateTime dateTime => dateTime.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture),
                IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture) ?? string.Empty,
                _ => part.ToString() ?? string.Empty,
            });
            BinaryPrimitives.WriteInt32BigEndian(length, bytes.Length);
            hash.AppendData(length);
            hash.AppendData(bytes);
        }

        return From(Convert.ToHexStringLower(hash.GetHashAndReset()));
    }

    private static string NormalizeInput(string input) => input.ToLowerInvariant();

    private static Validation Validate(string value) =>
        value.Length == Length && value.All(char.IsAsciiHexDigitLower)
            ? Validation.Ok
            : Validation.Invalid($"A SHA-256 digest is {Length} hexadecimal characters.");
}
