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

        if (_key.Length != 32)
            throw new InvalidOperationException("Crypto:Key must be 32 bytes (base64).");
    }

    public bool IsAllowedExtension(string fileName)
    {
        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        return Allowed.Contains(ext);
    }

    public bool IsAllowedSize(long fileSize)
    {
        return fileSize > 0 && fileSize <= 10 * 1024 * 1024; // 10MB
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
        await fs.WriteAsync(iv); // store IV first

        await using var cs = new CryptoStream(fs, aes.CreateEncryptor(), CryptoStreamMode.Write);
        await plain.CopyToAsync(cs);

        return storedName;
    }

    public async Task DecryptToAsync(string storedFileName, string targetFilePath)
    {
        var fullPath = Path.Combine(_uploadsDir, storedFileName);
        await using var fs = File.OpenRead(fullPath);

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

    // NEW — FIXED
    public async Task<Stream> DecryptAsync(string storedFileName)
    {
        var fullPath = Path.Combine(_uploadsDir, storedFileName);

        var output = new MemoryStream();
        await using var fs = File.OpenRead(fullPath);

        // read IV (first 16 bytes)
        var iv = new byte[16];
        _ = await fs.ReadAsync(iv);

        using var aes = Aes.Create();
        aes.Key = _key;
        aes.IV = iv;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        await using var cs = new CryptoStream(fs, aes.CreateDecryptor(), CryptoStreamMode.Read);
        await cs.CopyToAsync(output);

        output.Position = 0; // reset for caller
        return output;
    }
}
