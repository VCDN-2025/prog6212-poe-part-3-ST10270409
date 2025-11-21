namespace CMCS.Web.Services;

public interface IFileCrypto
{
    Task<string> EncryptAndSaveAsync(Stream plain, string targetDir, string originalName);
    Task DecryptToAsync(string storedFileName, string targetFilePath);
    bool IsAllowedExtension(string fileName);
    bool IsAllowedSize(long fileSize);
    Task<Stream> DecryptAsync(string storedFileName);
}