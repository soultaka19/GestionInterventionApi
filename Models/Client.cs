namespace GestionInterventionApi.Models;

public class Client : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string? City { get; set; }
    public string? PostalCode { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string? Notes { get; set; }

    // Navigation
    public Organization Organization { get; set; } = null!;
    public ICollection<Equipment> Equipments { get; set; } = new List<Equipment>();
}
