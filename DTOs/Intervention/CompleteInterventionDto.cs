using GestionInterventionApi.Models;

namespace GestionInterventionApi.DTOs.Intervention;

public record CompleteInterventionDto(
    string? TechnicianNotes,
    InterventionReport? Report
);
