using CMCS.Web.Services;
using FluentAssertions;

namespace CMCS.Tests;

public class UploadGuardsTests
{
    [Fact]
    public void Allowed_Extensions_Only()
    {
        var env = TestHelpers.TempWebHostEnv(out var root);
        try
        {
            var cfg = TestHelpers.CryptoConfig();
            var crypto = new AesFileCrypto(env, cfg);

            crypto.IsAllowedExtension("a.pdf").Should().BeTrue();
            crypto.IsAllowedExtension("a.docx").Should().BeTrue();
            crypto.IsAllowedExtension("a.xlsx").Should().BeTrue();

            crypto.IsAllowedExtension("a.exe").Should().BeFalse();
            crypto.IsAllowedExtension("a.jpg").Should().BeFalse();
            crypto.IsAllowedExtension("a.png").Should().BeFalse();
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }
}
