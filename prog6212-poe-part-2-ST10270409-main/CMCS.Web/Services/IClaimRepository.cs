using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CMCS.Web.Models
{
    public interface IClaimRepository
    {
        // Lecturer: “My Claims”
        Task<IReadOnlyList<Claim>> GetClaimsByLecturerAsync(string lecturerId);

        // HR dashboards / reports
        Task<IReadOnlyList<Claim>> GetAllAsync();

        // Coordinator dashboard (pending verification)
        Task<IReadOnlyList<Claim>> GetPendingForCoordinatorAsync();

        // Manager dashboard (awaiting final approval)
        Task<IReadOnlyList<Claim>> GetVerifiedForManagerAsync();

        // Common CRUD
        Task<Claim?> GetAsync(Guid id);
        Task AddAsync(Claim claim);
        Task UpdateAsync(Claim claim);
        Task DeleteAsync(Guid id);
    }
}
