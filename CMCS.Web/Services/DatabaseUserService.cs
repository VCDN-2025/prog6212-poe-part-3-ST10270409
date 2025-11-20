using CMCS.Web.Data;
using CMCS.Web.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;

namespace CMCS.Web.Services;

public class DatabaseUserService : IUserService
{
    private readonly AppDbContext _context;

    public DatabaseUserService(AppDbContext context)
    {
        _context = context;
    }

    public User? Authenticate(string email, string password)
    {
        var user = _context.Users
            .FirstOrDefault(u => u.Email == email);

        if (user == null)
            return null;

        // Handle Base64 encoded passwords from SeedData
        try
        {
            var decodedBytes = Convert.FromBase64String(user.PasswordHash);
            var storedPassword = System.Text.Encoding.UTF8.GetString(decodedBytes);

            if (password == storedPassword)
            {
                user.LastLogin = DateTime.UtcNow;
                _context.SaveChanges();
                return user;
            }
        }
        catch
        {
            // If Base64 decoding fails, try the original hashing method
            if (VerifyPassword(password, user.PasswordHash))
            {
                user.LastLogin = DateTime.UtcNow;
                _context.SaveChanges();
                return user;
            }
        }

        return null;
    }

    public User? GetUserById(Guid id)
    {
        return _context.Users.Find(id);
    }

    public List<User> GetAllUsers()
    {
        return _context.Users.ToList();
    }

    public void AddUser(User user)
    {
        // Simple password hashing for new users
        user.PasswordHash = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("TempPassword123!"));
        user.CreatedAt = DateTime.UtcNow;
        user.IsActive = true;

        _context.Users.Add(user);
        _context.SaveChanges();
    }

    public void UpdateUser(User user)
    {
        _context.Users.Update(user);
        _context.SaveChanges();
    }

    public void DeleteUser(Guid id)
    {
        var user = _context.Users.Find(id);
        if (user != null)
        {
            _context.Users.Remove(user);
            _context.SaveChanges();
        }
    }

    public bool ChangePassword(Guid userId, string currentPassword, string newPassword)
    {
        var user = _context.Users.Find(userId);
        if (user == null) return false;

        // Verify current password
        try
        {
            var decodedBytes = Convert.FromBase64String(user.PasswordHash);
            var storedPassword = System.Text.Encoding.UTF8.GetString(decodedBytes);

            if (currentPassword != storedPassword)
                return false;
        }
        catch
        {
            if (!VerifyPassword(currentPassword, user.PasswordHash))
                return false;
        }

        // Set new password
        user.PasswordHash = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(newPassword));
        user.MustChangePassword = false;
        _context.SaveChanges();
        return true;
    }

    public bool ResetPassword(Guid userId, string newPassword)
    {
        var user = _context.Users.Find(userId);
        if (user == null) return false;

        user.PasswordHash = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(newPassword));
        user.MustChangePassword = true;
        _context.SaveChanges();
        return true;
    }

    // Original password hashing method (for compatibility)
    private static string HashPassword(string password)
    {
        byte[] salt = new byte[128 / 8];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(salt);
        }

        string hashed = Convert.ToBase64String(KeyDerivation.Pbkdf2(
            password: password,
            salt: salt,
            prf: KeyDerivationPrf.HMACSHA256,
            iterationCount: 100000,
            numBytesRequested: 256 / 8));

        return $"{Convert.ToBase64String(salt)}.{hashed}";
    }

    // Original password verification method (for compatibility)
    private static bool VerifyPassword(string password, string storedHash)
    {
        try
        {
            var parts = storedHash.Split('.', 2);
            if (parts.Length != 2) return false;

            var salt = Convert.FromBase64String(parts[0]);
            var storedSubkey = parts[1];

            var computedSubkey = Convert.ToBase64String(KeyDerivation.Pbkdf2(
                password: password,
                salt: salt,
                prf: KeyDerivationPrf.HMACSHA256,
                iterationCount: 100000,
                numBytesRequested: 256 / 8));

            return storedSubkey == computedSubkey;
        }
        catch
        {
            return false;
        }
    }
}