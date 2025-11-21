using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Moq;

// Alias the Microsoft configuration namespace to avoid Castle.Core conflicts
using MEConf = Microsoft.Extensions.Configuration;

namespace CMCS.Tests;

public static class TestHelpers
{
    public static IWebHostEnvironment TempWebHostEnv(out string root)
    {
        root = Path.Combine(Path.GetTempPath(), "cmcs-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);

        var env = new Mock<IWebHostEnvironment>();
        env.Setup(x => x.ContentRootPath).Returns(root);
        env.Setup(x => x.WebRootPath).Returns(root); // for uploads

        return env.Object;
    }

    // Note: return type is MEConf.IConfiguration (NOT IConfiguration)
    public static MEConf.IConfiguration CryptoConfig(string? keyB64 = null)
    {
        keyB64 ??= Convert.ToBase64String(new byte[32]);
        var dict = new Dictionary<string, string?> { ["Crypto:Key"] = keyB64 };

        // Also use the alias for the builder
        return new MEConf.ConfigurationBuilder()
            .AddInMemoryCollection(dict!)
            .Build();
    }
}