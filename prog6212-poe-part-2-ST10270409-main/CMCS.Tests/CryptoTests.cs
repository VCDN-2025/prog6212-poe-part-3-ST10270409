using CMCS.Web.Services;
using FluentAssertions;

namespace CMCS.Tests;

public class CryptoTests
{
    [Fact]
    public async Task Aes_Encrypt_Then_Decrypt_RoundTrip()
    {
        var env = TestHelpers.TempWebHostEnv(out var root);
        try
        {
            var cfg = TestHelpers.CryptoConfig(); // 32-byte zero key (for tests)
            var crypto = new AesFileCrypto(env, cfg);

            var payloadText = "hello CMCS";
            await using var ms = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(payloadText));

            var stored = await crypto.EncryptAndSaveAsync(ms, Path.Combine(root, "uploads"), "a.pdf");
            var outPath = Path.Combine(root, "decrypted.tmp");

            await crypto.DecryptToAsync(stored, outPath);

            var text = await File.ReadAllTextAsync(outPath);
            text.Should().Be(payloadText);
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }
}
