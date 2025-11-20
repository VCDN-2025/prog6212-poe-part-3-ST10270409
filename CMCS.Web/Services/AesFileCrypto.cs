using System.Security.Cryptography;

namespace CMCS.Web.Services;

public sealed class AesFileCrypto : IFileCrypto
{
    private static readonly HashSet<string> Allowed = new(StringComparer.OrdinalIgnoreCase)
        { ".pdf", ".docx", ".xlsx", ".doc", ".xls", ".jpg", ".jpeg", ".png", ".txt" };

    private readonly string _uploadsDir;
    private readonly byte[] _key;

    public AesFileCrypto(IWebHostEnvironment env, IConfiguration cfg)
    {
        _uploadsDir = Path.Combine(env.WebRootPath, "uploads");
        Directory.CreateDirectory(_uploadsDir);

        var keyB64 = cfg["Crypto:Key"] ?? "";
        _key = Convert.FromBase64String(keyB64);
        if (_key.Length != 32) throw new InvalidOperationException("Crypto:Key must be 32 bytes (base64).");
    }

    public bool IsAllowedExtension(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return Allowed.Contains(extension);
    }

    public bool IsAllowedSize(long fileSize)
    {
        // Allow files up to 10MB (instead of 2MB)
        return fileSize > 0 && fileSize <= 10 * 1024 * 1024; // 10MB in bytes
    }

    public async Task<string> EncryptAndSaveAsync(Stream plain, string targetDir, string originalName)
    {
        Directory.CreateDirectory(targetDir);
        var iv = RandomNumberGenerator.GetBytes(16);
        var storedName = $"{Guid.NewGuid():N}.bin";
        var path = Path.Combine(targetDir, storedName);

        using var aes = Aes.Create();
        aes.Key = _key;
        aes.IV = iv;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        await using var fs = File.Create(path);
        await fs.WriteAsync(iv); // prefix IV
        await using var cs = new CryptoStream(fs, aes.CreateEncryptor(), CryptoStreamMode.Write);
        await plain.CopyToAsync(cs);

        return storedName;
    }

    public async Task DecryptToAsync(string storedFileName, string targetFilePath)
    {
        var path = Path.Combine(_uploadsDir, storedFileName);
        await using var fs = File.OpenRead(path);

        var iv = new byte[16];
        _ = await fs.ReadAsync(iv);

        using var aes = Aes.Create();
        aes.Key = _key;
        aes.IV = iv;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        await using var cs = new CryptoStream(fs, aes.CreateDecryptor(), CryptoStreamMode.Read);
        await using var outFs = File.Create(targetFilePath);
        await cs.CopyToAsync(outFs);
    }
}