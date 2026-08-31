using GestionInterventionApi.Models.Enums;

namespace GestionInterventionApi.Models;

public class Organization
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public SubscriptionPlan SubscriptionPlan { get; set; } = SubscriptionPlan.Free;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Organisation de démonstration, créée à la volée pour un visiteur.
    /// Elle est supprimée automatiquement passé <see cref="ExpiresAt"/>.
    /// </summary>
    public bool IsDemo { get; set; }

    /// <summary>
    /// Date d'expiration d'un bac à sable. Nulle pour une organisation réelle,
    /// qui n'expire jamais.
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    // Navigation
    public ICollection<User> Users { get; set; } = new List<User>();
}
