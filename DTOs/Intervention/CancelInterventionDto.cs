namespace GestionInterventionApi.DTOs.Intervention;

/// <summary>
/// Corps de <c>POST /api/interventions/{id}/cancel</c>.
///
/// B-5 : l'action prenait auparavant `[FromBody] string? reason`. Un corps JSON
/// d'objet — `{"reason": "client absent"}`, la seule forme qu'un client HTTP
/// normal envoie — produisait un 400, car la liaison attendait une chaine JSON
/// nue (`"client absent"`). L'action etait donc inutilisable telle que
/// documentee. Un DTO regle le probleme et laisse la place a une validation.
/// </summary>
public record CancelInterventionDto(string? Reason);
