using CMCS.Web.Models;

namespace CMCS.Web.Services
{
    public interface IUserService
    {
        User? Authenticate(string email, string password);
        IEnumerable<User> GetAllUsers();
        User? GetUserById(Guid id);
        void AddUser(User user);
        void UpdateUser(User user);
        void DeleteUser(Guid id);
        void ResetPassword(Guid userId, string newPassword);
        bool ChangePassword(Guid userId, string currentPassword, string newPassword);
    }
}
