namespace GestionInterventionApi.Models.Enums;

public enum InterventionStatus
{
    Pending = 0,        // En attente de planification
    Scheduled = 1,      // Planifiée
    InProgress = 2,     // En cours
    Completed = 3,      // Terminée
    Cancelled = 4       // Annulée
}
