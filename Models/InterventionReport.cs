namespace GestionInterventionApi.Models;

public class InterventionReport
{
    // Checklist standard
    public List<ChecklistItem> Checklist { get; set; } = new();

    // Photos prises pendant l'intervention
    public List<PhotoItem> Photos { get; set; } = new();

    // Signature du client
    public SignatureData? ClientSignature { get; set; }

    // Pièces utilisées
    public List<PartUsed> PartsUsed { get; set; } = new();

    // Mesures et relevés
    public Dictionary<string, string> Measurements { get; set; } = new();

    // Commentaires du technicien
    public string? TechnicianComments { get; set; }

    // Recommandations
    public string? Recommendations { get; set; }
}

public class ChecklistItem
{
    public string Label { get; set; } = string.Empty;
    public bool Checked { get; set; }
    public string? Comment { get; set; }
}

public class PhotoItem
{
    public string Url { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime TakenAt { get; set; }
}

public class SignatureData
{
    public string DataUrl { get; set; } = string.Empty; // Base64 de la signature
    public string SignatoryName { get; set; } = string.Empty;
    public DateTime SignedAt { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
}

public class PartUsed
{
    public string PartNumber { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal? UnitPrice { get; set; }
}
