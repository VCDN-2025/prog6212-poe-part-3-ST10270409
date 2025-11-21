using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using CMCS.Web.Data;
using CMCS.Web.Models;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using Microsoft.EntityFrameworkCore;

namespace CMCS.Web.Services
{
    public class DatabaseUserService : IUserService
    {
        private readonly AppDbContext _context;

        public DatabaseUserService(AppDbContext context)
        {
            _context = context;
        }

        // In-text reference: (Microsoft, 2024) User authentication and security fundamentals.
        // URL: https://learn.microsoft.com/aspnet/core/security/
        public User? Authenticate(string email, string password)
        {
            var user = _context.Users
                .FirstOrDefault(u => u.Email == email);

            if (user == null)
                return null;

            // 1) Try Base64-encoded plain-text password (for seeded users / temp passwords)
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
                // Ignore and try PBKDF2 below
            }

            // 2) Try original PBKDF2 hashing format (salt.hash)
            if (VerifyPassword(password, user.PasswordHash))
            {
                user.LastLogin = DateTime.UtcNow;
                _context.SaveChanges();
                return user;
            }

            return null;
        }

        public User? GetUserById(Guid id)
        {
            return _context.Users.Find(id);
        }

        public IEnumerable<User> GetAllUsers()
        {
            return _context.Users.ToList();
        }

        // In-text reference: (Microsoft, 2024) Data access with EF Core.
        // URL: https://learn.microsoft.com/aspnet/core/data/ef-mvc/intro
        public void AddUser(User user)
        {
            // Default temporary password if none is provided anywhere else
            if (string.IsNullOrWhiteSpace(user.PasswordHash))
            {
                var tempPassword = "TempPassword123!";
                user.PasswordHash = Convert.ToBase64String(
                    System.Text.Encoding.UTF8.GetBytes(tempPassword));
                user.MustChangePassword = true;
            }

            user.CreatedAt = DateTime.UtcNow;
            if (!user.IsActive)
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
            if (user == null) return;

            _context.Users.Remove(user);
            _context.SaveChanges();
        }

        // In-text reference: (Microsoft, 2024) Password management and reset flows.
        // URL: https://learn.microsoft.com/aspnet/core/security/authentication/identity#passwords
        public void ResetPassword(Guid userId, string newPassword)
        {
            var user = _context.Users.Find(userId);
            if (user == null) return;

            // Store new temporary password as Base64-encoded string
            user.PasswordHash = Convert.ToBase64String(
                System.Text.Encoding.UTF8.GetBytes(newPassword));

            user.MustChangePassword = true;
            _context.SaveChanges();
        }

        // In-text reference: (Microsoft, 2024) Securely handling user passwords.
        // URL: https://learn.microsoft.com/aspnet/core/security/authentication/identity#password-hashing
        public bool ChangePassword(Guid userId, string currentPassword, string newPassword)
        {
            var user = _context.Users.Find(userId);
            if (user == null) return false;

            var currentValid = false;

            // 1) First try Base64-encoded stored password
            try
            {
                var decodedBytes = Convert.FromBase64String(user.PasswordHash);
                var storedPassword = System.Text.Encoding.UTF8.GetString(decodedBytes);

                if (currentPassword == storedPassword)
                {
                    currentValid = true;
                }
            }
            catch
            {
                // 2) Fallback to PBKDF2 verification format
                if (VerifyPassword(currentPassword, user.PasswordHash))
                    currentValid = true;
            }

            if (!currentValid)
                return false;

            // If current password verified, update to new password (Base64)
            user.PasswordHash = Convert.ToBase64String(
                System.Text.Encoding.UTF8.GetBytes(newPassword));

            user.MustChangePassword = false;
            _context.SaveChanges();
            return true;
        }

        // PBKDF2 hashing (legacy support for older users / formats)
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
}
