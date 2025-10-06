using Engitrack.BuildingBlocks.Domain;
using Engitrack.Projects.Domain.Enums;

namespace Engitrack.Projects.Domain.Entities;

public class User : Entity
{
    public string Email { get; private set; } = string.Empty;
    public string FullName { get; private set; } = string.Empty;
    public string Phone { get; private set; } = string.Empty;
    public Role Role { get; private set; }

    private User() { } // EF Constructor

    public User(string email, string fullName, string phone, Role role)
    {
        if (string.IsNullOrWhiteSpace(email) || email.Length > 256)
            throw new ArgumentException("Email is required and must be <= 256 characters", nameof(email));
        
        if (string.IsNullOrWhiteSpace(fullName) || fullName.Length > 120)
            throw new ArgumentException("FullName is required and must be <= 120 characters", nameof(fullName));
        
        if (string.IsNullOrWhiteSpace(phone) || phone.Length > 32)
            throw new ArgumentException("Phone is required and must be <= 32 characters", nameof(phone));

        Email = email;
        FullName = fullName;
        Phone = phone;
        Role = role;
    }

    public void UpdateProfile(string fullName, string phone)
    {
        if (string.IsNullOrWhiteSpace(fullName) || fullName.Length > 120)
            throw new ArgumentException("FullName is required and must be <= 120 characters", nameof(fullName));
        
        if (string.IsNullOrWhiteSpace(phone) || phone.Length > 32)
            throw new ArgumentException("Phone is required and must be <= 32 characters", nameof(phone));

        FullName = fullName;
        Phone = phone;
        MarkAsUpdated();
    }

    public void ChangeRole(Role newRole)
    {
        Role = newRole;
        MarkAsUpdated();
    }
}