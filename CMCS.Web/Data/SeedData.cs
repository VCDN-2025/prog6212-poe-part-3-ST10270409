using CMCS.Web.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;

namespace CMCS.Web.Data;

public static class SeedData
{
    public static void Initialize(IServiceProvider serviceProvider)
    {
        using var context = new AppDbContext(
            serviceProvider.GetRequiredService<DbContextOptions<AppDbContext>>());

        context.Database.EnsureCreated();

        if (!context.Users.Any())
        {
            var users = new[]
            {
                new User
                {
                    Id = Guid.NewGuid(),
                    Email = "lecturer@campus.com",
                    PasswordHash = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("lecturer123")),
                    Role = "Lecturer",
                    Name = "Dr. Lonwabo Wabo",
                    HourlyRate = 350.00m,
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                },
                new User
                {
                    Id = Guid.NewGuid(),
                    Email = "coordinator@campus.com",
                    PasswordHash = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("coordinator123")),
                    Role = "Coordinator",
                    Name = "Ms. Sarah Johnson",
                    HourlyRate = 0,
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                },
                new User
                {
                    Id = Guid.NewGuid(),
                    Email = "manager@campus.com",
                    PasswordHash = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("manager123")),
                    Role = "Manager",
                    Name = "Mr. David Wilson",
                    HourlyRate = 0,
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                },
                new User
                {
                    Id = Guid.NewGuid(),
                    Email = "hr@campus.com",
                    PasswordHash = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("hr123")),
                    Role = "HR",
                    Name = "HR Department",
                    HourlyRate = 0,
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                }
            };

            foreach (var user in users)
            {
                context.Users.Add(user);
            }

            context.SaveChanges();
        }
    }
}