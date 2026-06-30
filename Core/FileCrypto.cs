using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace MindedOS.Core;

/// <summary>
/// Decrypts EEG1-format files (AES-256-GCM with a PBKDF2-HMAC-SHA256 key),
/// byte-for-byte compatible with the web crypter (crypter.php) used to produce
/// eeg_map.encrypted.csv. The lexicon passphrase is embedded here so it ships
/// compiled inside MindedOS.dll — the CSV stays encrypted at rest on disk.
///
/// On-disk layout: "EEG1" | salt(16) | iv(12) | ciphertext | tag(16).
/// </summary>
public static class FileCrypto
{
    /// <summary>Embedded AES passphrase for the bundled eeg_map.encrypted.csv.</summary>
    public const string LexiconKey = ")O()I()U89uy&*Y&GHHGY&Ti";

    private static readonly byte[] Magic = { 0x45, 0x45, 0x47, 0x31 }; // "EEG1"
    private const int SaltBytes = 16;
    private const int IvBytes = 12;
    private const int TagBytes = 16;
    private const int Pbkdf2Iterations = 120_000;
    private const int KeyBytes = 32; // AES-256
    private const int HeaderBytes = 4 + SaltBytes + IvBytes;

    /// <summary>True if the blob starts with the "EEG1" magic header.</summary>
    public static bool LooksEncrypted(ReadOnlySpan<byte> bytes) =>
        bytes.Length >= 4 &&
        bytes[0] == Magic[0] && bytes[1] == Magic[1] &&
        bytes[2] == Magic[2] && bytes[3] == Magic[3];

    /// <summary>
    /// Decrypt EEG1 bytes to UTF-8 text. Throws <see cref="InvalidDataException"/>
    /// on a wrong key or a corrupt/short file.
    /// </summary>
    public static string DecryptToText(byte[] blob, string passphrase)
    {
        if (!LooksEncrypted(blob))
            throw new InvalidDataException("Not an EEG1-format file.");
        if (blob.Length < HeaderBytes + TagBytes)
            throw new InvalidDataException("EEG1 file is too short to be valid.");

        var salt = new byte[SaltBytes];
        Array.Copy(blob, Magic.Length, salt, 0, SaltBytes);
        var iv = new byte[IvBytes];
        Array.Copy(blob, Magic.Length + SaltBytes, iv, 0, IvBytes);

        int cipherLen = blob.Length - HeaderBytes - TagBytes;
        var cipher = new byte[cipherLen];
        Array.Copy(blob, HeaderBytes, cipher, 0, cipherLen);
        var tag = new byte[TagBytes];
        Array.Copy(blob, HeaderBytes + cipherLen, tag, 0, TagBytes);

        byte[] key = Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(passphrase), salt,
            Pbkdf2Iterations, HashAlgorithmName.SHA256, KeyBytes);

        var plain = new byte[cipherLen];
        try
        {
            using var gcm = new AesGcm(key, TagBytes);
            gcm.Decrypt(iv, cipher, tag, plain);
        }
        catch (CryptographicException)
        {
            throw new InvalidDataException("Wrong key, or the file is corrupt.");
        }
        return Encoding.UTF8.GetString(plain);
    }

    /// <summary>
    /// Read a CSV that may be EEG1-encrypted or plaintext and return its text.
    /// Encrypted files are decrypted with the embedded <see cref="LexiconKey"/>.
    /// </summary>
    public static string ReadTextMaybeEncrypted(string path)
    {
        var raw = File.ReadAllBytes(path);
        if (LooksEncrypted(raw))
            return DecryptToText(raw, LexiconKey);

        // Plaintext: honour a UTF-8 BOM if present.
        return raw.Length >= 3 && raw[0] == 0xEF && raw[1] == 0xBB && raw[2] == 0xBF
            ? Encoding.UTF8.GetString(raw, 3, raw.Length - 3)
            : Encoding.UTF8.GetString(raw);
    }
}
