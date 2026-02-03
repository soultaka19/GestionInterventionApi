using GestionInterventionApi.Models.Enums;

namespace GestionInterventionApi.Models;

public class Equipment : BaseEntity
{
    public Guid ClientId { get; set; }
    public EquipmentType Type { get; set; }
    public string Brand { get; set; } = string.Empty;
    public string? Model { get; set; }
    public string? SerialNumber { get; set; }
    public DateTime? InstallationDate { get; set; }
    public DateTime? LastMaintenanceDate { get; set; }
    public DateTime? WarrantyEndDate { get; set; }
    public string? Notes { get; set; }

    // Navigation
    public Client Client { get; set; } = null!;
    public Organization Organization { get; set; } = null!;
}
