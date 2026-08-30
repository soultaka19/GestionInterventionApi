using System.Text.Json;
using GestionInterventionApi.DTOs.Location;

namespace GestionInterventionApi.Services;

public class GeocodingService : IGeocodingService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<GeocodingService> _logger;

    public GeocodingService(HttpClient httpClient, IConfiguration configuration, ILogger<GeocodingService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<GeocodeResultDto> GeocodeAddressAsync(string address)
    {
        var apiKey = _configuration["GoogleMaps:ApiKey"];

        if (string.IsNullOrEmpty(apiKey))
        {
            _logger.LogWarning("Google Maps API key not configured");
            return new GeocodeResultDto(0, 0, "", false, "Google Maps API key not configured");
        }

        try
        {
            var encodedAddress = Uri.EscapeDataString(address);
            var url = $"https://maps.googleapis.com/maps/api/geocode/json?address={encodedAddress}&key={apiKey}";

            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            using var document = JsonDocument.Parse(content);
            var root = document.RootElement;

            var status = root.GetProperty("status").GetString();
            if (status != "OK")
            {
                return new GeocodeResultDto(0, 0, "", false, $"Geocoding failed: {status}");
            }

            var result = root.GetProperty("results")[0];
            var location = result.GetProperty("geometry").GetProperty("location");
            var formattedAddress = result.GetProperty("formatted_address").GetString() ?? address;

            return new GeocodeResultDto(
                location.GetProperty("lat").GetDouble(),
                location.GetProperty("lng").GetDouble(),
                formattedAddress,
                true
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error geocoding address: {Address}", address);
            return new GeocodeResultDto(0, 0, "", false, ex.Message);
        }
    }

    public async Task<string> ReverseGeocodeAsync(double latitude, double longitude)
    {
        var apiKey = _configuration["GoogleMaps:ApiKey"];

        if (string.IsNullOrEmpty(apiKey))
        {
            _logger.LogWarning("Google Maps API key not configured");
            return "Address unavailable";
        }

        try
        {
            // B-9 : invariant obligatoire — en fr-CA, `{latitude},{longitude}`
            // produirait « 45,42,-75,69 » et Google renverrait ZERO_RESULTS.
            var latlng = FormattableString.Invariant($"{latitude},{longitude}");
            var url = $"https://maps.googleapis.com/maps/api/geocode/json?latlng={latlng}&key={apiKey}";

            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            using var document = JsonDocument.Parse(content);
            var root = document.RootElement;

            var status = root.GetProperty("status").GetString();
            if (status != "OK")
            {
                return "Address unavailable";
            }

            return root.GetProperty("results")[0].GetProperty("formatted_address").GetString() ?? "Address unavailable";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reverse geocoding: {Lat}, {Lng}", latitude, longitude);
            return "Address unavailable";
        }
    }
}
