using GestionInterventionApi.DTOs.Location;

namespace GestionInterventionApi.Services;

public interface IGeocodingService
{
    Task<GeocodeResultDto> GeocodeAddressAsync(string address);
    Task<string> ReverseGeocodeAsync(double latitude, double longitude);
}
