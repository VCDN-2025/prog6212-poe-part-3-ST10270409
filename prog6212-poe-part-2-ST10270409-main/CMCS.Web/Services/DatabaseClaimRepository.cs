using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CMCS.Web.Data;
using CMCS.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace CMCS.Web.Services
{
    public sealed class DatabaseClaimRepository : IClaimRepository
    {
        private readonly AppDbContext _db;

        public DatabaseClaimRepository(AppDbContext db)
        {
            _db = db;
        }

        // ======== LISTS ========

        // All claims – HR / reports
        public async Task<IReadOnlyList<Claim>> GetAllAsync()
        {
            return await _db.Claims
                .Include(c => c.Documents)
                .OrderByDescending(c => c.Date)
                .ToListAsync();
        }

        // “My Claims” – Lecturer
        public async Task<IReadOnlyList<Claim>> GetClaimsByLecturerAsync(string lecturerId)
        {
            return await _db.Claims
                .Include(c => c.Documents)
                .Where(c => c.LecturerId == lecturerId)
                .OrderByDescending(c => c.Date)
                .ToListAsync();
        }

        // Coordinator – claims still pending verification
        public async Task<IReadOnlyList<Claim>> GetPendingForCoordinatorAsync()
        {
            return await _db.Claims
                .Include(c => c.Documents)
                .Where(c => c.Status == ClaimStatus.Pending)
                .OrderBy(c => c.Date)
                .ToListAsync();
        }

        // Manager – verified by coordinator, waiting for final approval
        public async Task<IReadOnlyList<Claim>> GetVerifiedForManagerAsync()
        {
            return await _db.Claims
                .Include(c => c.Documents)
                .Where(c => c.Status == ClaimStatus.VerifiedByCoordinator)
                .OrderBy(c => c.Date)
                .ToListAsync();
        }

        // ======== CRUD ========

        public async Task<Claim?> GetAsync(Guid id)
        {
            return await _db.Claims
                .Include(c => c.Documents)
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task AddAsync(Claim claim)
        {
            _db.Claims.Add(claim);
            await _db.SaveChangesAsync();
        }

        public async Task UpdateAsync(Claim claim)
        {
            _db.Claims.Update(claim);
            await _db.SaveChangesAsync();
        }

        public async Task DeleteAsync(Guid id)
        {
            var claim = await _db.Claims.FindAsync(id);
            if (claim != null)
            {
                _db.Claims.Remove(claim);
                await _db.SaveChangesAsync();
            }
        }
    }
}
