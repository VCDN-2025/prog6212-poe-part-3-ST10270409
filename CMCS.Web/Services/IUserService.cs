using CMCS.Web.Models;

namespace CMCS.Web.Services;

public interface IUserService
{
    User? Authenticate(string email, string password);
    User? GetUserById(Guid id);
    List<User> GetAllUsers();
    void AddUser(User user);
    void UpdateUser(User user);
    void DeleteUser(Guid id);
    bool ChangePassword(Guid userId, string currentPassword, string newPassword);
    bool ResetPassword(Guid userId, string newPassword);
}