namespace GestionInterventionApi.DTOs.Location;

public record TechnicianLocationDto(
    Guid TechnicianId,
    string TechnicianName,
    double Latitude,
    double Longitude,
    double? Accuracy,
    double? Speed,
    double? Heading,
    DateTime Timestamp,
    bool IsOnline
);
