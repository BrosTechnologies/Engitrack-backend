using Engitrack.BuildingBlocks.Domain;

namespace Engitrack.Projects.Domain.Entities;

public enum MachineStatus
{
    OPERATIONAL,
    UNDER_MAINTENANCE,
    AVAILABLE
}

public class Machine : Entity
{
    public Guid ProjectId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string SerialNumber { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public MachineStatus Status { get; set; }
    public DateTime? LastMaintenanceDate { get; set; }
    public DateTime? NextMaintenanceDate { get; set; }
    public decimal? HourlyRate { get; set; }
    public byte[] RowVersion { get; set; } = null!;

    // Navigation property
    public Project Project { get; set; } = null!;
}