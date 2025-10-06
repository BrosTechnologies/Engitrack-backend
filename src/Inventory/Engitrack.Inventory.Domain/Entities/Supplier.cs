using Engitrack.BuildingBlocks.Domain;

namespace Engitrack.Inventory.Domain.Entities;

public class Supplier : Entity
{
    public string Name { get; private set; } = string.Empty;
    public string ContactInfo { get; private set; } = string.Empty;
    public string Address { get; private set; } = string.Empty;

    private Supplier() { } // EF Constructor

    public Supplier(string name, string contactInfo, string address)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Length > 160)
            throw new ArgumentException("Name is required and must be <= 160 characters", nameof(name));

        Name = name;
        ContactInfo = contactInfo ?? string.Empty;
        Address = address ?? string.Empty;
    }

    public void UpdateInfo(string name, string contactInfo, string address)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Length > 160)
            throw new ArgumentException("Name is required and must be <= 160 characters", nameof(name));

        Name = name;
        ContactInfo = contactInfo ?? string.Empty;
        Address = address ?? string.Empty;
        MarkAsUpdated();
    }
}