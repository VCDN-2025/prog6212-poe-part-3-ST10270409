using CMCS.Web.Models;

namespace CMCS.Web.Services;

public interface IClaimRepository
{
    // Synchronous methods
    List<Claim> GetAll();
    Claim? Get(Guid id);
    void Add(Claim claim);
    void Update(Claim claim);

    // Asynchronous methods
    Task<List<Claim>> GetAllAsync();
    Task<Claim?> GetAsync(Guid id);
    Task AddAsync(Claim claim);
    Task UpdateAsync(Claim claim);

    // Enhanced methods
    Task<List<Claim>> GetClaimsByStatusAsync(ClaimStatus status);
    Task<List<Claim>> GetClaimsByLecturerAsync(string lecturerId);
}