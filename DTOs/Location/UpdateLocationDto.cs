namespace GestionInterventionApi.DTOs.Location;

public record UpdateLocationDto(
    double Latitude,
    double Longitude,
    double? Accuracy = null,
    double? Speed = null,
    double? Heading = null,
    Guid? TechnicianId = null
);
