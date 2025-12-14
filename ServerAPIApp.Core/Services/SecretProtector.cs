using ServerAPIApp.Core.Abstractions;
using System.Security.Cryptography;
using System.Text;


namespace ServerAPIApp.Core.Services;

public class SecretProtector : ISecretProtector
{
    private readonly byte[] _key;
    private int _tagSizeBytes = 16;

    public SecretProtector(byte[] key)
    {
        if (key == null) throw new ArgumentNullException(nameof(key));
        if (!(key.Length == 16 || key.Length == 24 || key.Length == 32))
            throw new ArgumentException("Key length must be 16, 24 or 32 bytes for AES.", nameof(key));
        _key = key;
    }

    // Формат выходной строки (base64): [nonce 12b] + [ciphertext] + [tag 16b]
    public string Protect(string plain)
    {
        if (plain is null) throw new ArgumentNullException(nameof(plain));
        var plainBytes = Encoding.UTF8.GetBytes(plain);

        var nonce = new byte[12];
        RandomNumberGenerator.Fill(nonce);

        var ciphertext = new byte[plainBytes.Length];
        var tag = new byte[16];

        using (var aes = new AesGcm(_key, _tagSizeBytes))
        {
            aes.Encrypt(nonce, plainBytes, ciphertext, tag, null);
        }

        var outBytes = new byte[nonce.Length + ciphertext.Length + tag.Length];
        Buffer.BlockCopy(nonce, 0, outBytes, 0, nonce.Length);
        Buffer.BlockCopy(ciphertext, 0, outBytes, nonce.Length, ciphertext.Length);
        Buffer.BlockCopy(tag, 0, outBytes, nonce.Length + ciphertext.Length, tag.Length);

        return Convert.ToBase64String(outBytes);
    }

    public string Unprotect(string protectedText)
    {
        if (protectedText is null) throw new ArgumentNullException(nameof(protectedText));
        var all = Convert.FromBase64String(protectedText);

        if (all.Length < 12 + 16) throw new CryptographicException("Invalid protected payload.");

        var nonce = new byte[12];
        Buffer.BlockCopy(all, 0, nonce, 0, nonce.Length);

        var tag = new byte[16];
        Buffer.BlockCopy(all, all.Length - tag.Length, tag, 0, tag.Length);

        var ciphertextLen = all.Length - nonce.Length - tag.Length;
        var ciphertext = new byte[ciphertextLen];
        Buffer.BlockCopy(all, nonce.Length, ciphertext, 0, ciphertextLen);

        var plaintext = new byte[ciphertextLen];
        using (var aes = new AesGcm(_key, _tagSizeBytes))
        {
            aes.Decrypt(nonce, ciphertext, tag, plaintext, null);
        }

        return Encoding.UTF8.GetString(plaintext);
    }
}
