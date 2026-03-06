using System.Security.Cryptography;
using System.Text;

namespace client_web.Helpers;

public static class EncryptData
{
    /// <summary>
    /// Genera el hash SHA-256 de un texto en formato hexadecimal (minúsculas),
    /// igual que el comportamiento de crypto.subtle en el cliente TypeScript.
    /// </summary>
    public static string Sha256(string input)
    {
        var bytes = Encoding.UTF8.GetBytes(input);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}