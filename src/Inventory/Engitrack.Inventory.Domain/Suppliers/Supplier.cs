using Engitrack.BuildingBlocks.Domain;

namespace Engitrack.Inventory.Domain.Suppliers;

public class Supplier : Entity
{
    public Guid SupplierId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Ruc { get; private set; }
    public string? Phone { get; private set; }
    public string? Email { get; private set; }

    private Supplier() { } // EF Constructor

    public Supplier(string name, string? ruc = null, string? phone = null, string? email = null)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Length > 160)
            throw new ArgumentException("Name is required and must be <= 160 characters", nameof(name));

        if (!string.IsNullOrEmpty(ruc) && ruc.Length > 20)
            throw new ArgumentException("Ruc must be <= 20 characters", nameof(ruc));

        if (!string.IsNullOrEmpty(phone) && phone.Length > 32)
            throw new ArgumentException("Phone must be <= 32 characters", nameof(phone));

        if (!string.IsNullOrEmpty(email) && email.Length > 160)
            throw new ArgumentException("Email must be <= 160 characters", nameof(email));

        SupplierId = Guid.NewGuid();
        Name = name;
        Ruc = ruc;
        Phone = phone;
        Email = email;
    }

    public void UpdateInfo(string name, string? ruc = null, string? phone = null, string? email = null)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Length > 160)
            throw new ArgumentException("Name is required and must be <= 160 characters", nameof(name));

        if (!string.IsNullOrEmpty(ruc) && ruc.Length > 20)
            throw new ArgumentException("Ruc must be <= 20 characters", nameof(ruc));

        if (!string.IsNullOrEmpty(phone) && phone.Length > 32)
            throw new ArgumentException("Phone must be <= 32 characters", nameof(phone));

        if (!string.IsNullOrEmpty(email) && email.Length > 160)
            throw new ArgumentException("Email must be <= 160 characters", nameof(email));

        Name = name;
        Ruc = ruc;
        Phone = phone;
        Email = email;
        MarkAsUpdated();
    }
}