using CMCS.Web.Data;
using CMCS.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace CMCS.Web.Services;

public class DatabaseClaimRepository : IClaimRepository
{
    private readonly AppDbContext _context;

    public DatabaseClaimRepository(AppDbContext context)
    {
        _context = context;
    }

    // Synchronous implementations
    public List<Claim> GetAll()
    {
        return _context.Claims
            .Include(c => c.Documents)
            .OrderByDescending(c => c.Date)
            .ToList();
    }

    public Claim? Get(Guid id)
    {
        return _context.Claims
            .Include(c => c.Documents)
            .FirstOrDefault(c => c.Id == id);
    }

    public void Add(Claim claim)
    {
        _context.Claims.Add(claim);
        _context.SaveChanges();
    }

    public void Update(Claim claim)
    {
        _context.Claims.Update(claim);
        _context.SaveChanges();
    }

    // Async implementations
    public async Task<List<Claim>> GetAllAsync()
    {
        return await _context.Claims
            .Include(c => c.Documents)
            .OrderByDescending(c => c.Date)
            .ToListAsync();
    }

    public async Task<Claim?> GetAsync(Guid id)
    {
        return await _context.Claims
            .Include(c => c.Documents)
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task AddAsync(Claim claim)
    {
        _context.Claims.Add(claim);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Claim claim)
    {
        _context.Claims.Update(claim);
        await _context.SaveChangesAsync();
    }

    public async Task<List<Claim>> GetClaimsByStatusAsync(ClaimStatus status)
    {
        return await _context.Claims
            .Where(c => c.Status == status)
            .Include(c => c.Documents)
            .OrderByDescending(c => c.Date)
            .ToListAsync();
    }

    public async Task<List<Claim>> GetClaimsByLecturerAsync(string lecturerId)
    {
        return await _context.Claims
            .Where(c => c.LecturerId == lecturerId)
            .Include(c => c.Documents)
            .OrderByDescending(c => c.Date)
            .ToListAsync();
    }
}