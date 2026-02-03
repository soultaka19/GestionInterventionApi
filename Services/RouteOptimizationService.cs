using System.Text.Json;
using GestionInterventionApi.DTOs.Location;

namespace GestionInterventionApi.Services;

public class RouteOptimizationService : IRouteOptimizationService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<RouteOptimizationService> _logger;

    public RouteOptimizationService(HttpClient httpClient, IConfiguration configuration, ILogger<RouteOptimizationService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<RouteOptimizationResultDto> OptimizeRouteAsync(RouteOptimizationRequestDto request)
    {
        var apiKey = _configuration["GoogleMaps:ApiKey"];

        if (string.IsNullOrEmpty(apiKey))
        {
            _logger.LogWarning("Google Maps API key not configured");
            return new RouteOptimizationResultDto(new List<RouteStepDto>(), 0, 0, false, "Google Maps API key not configured");
        }

        try
        {
            var origin = $"{request.Origin.Latitude},{request.Origin.Longitude}";
            var destination = request.Destination != null
                ? $"{request.Destination.Latitude},{request.Destination.Longitude}"
                : origin; // Retour au point de départ si pas de destination

            var waypoints = string.Join("|", request.Waypoints.Select(w => $"{w.Latitude},{w.Longitude}"));

            var url = $"https://maps.googleapis.com/maps/api/directions/json?" +
                      $"origin={origin}&destination={destination}" +
                      $"&waypoints=optimize:true|{waypoints}" +
                      $"&key={apiKey}";

            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            using var document = JsonDocument.Parse(content);
            var root = document.RootElement;

            var status = root.GetProperty("status").GetString();
            if (status != "OK")
            {
                return new RouteOptimizationResultDto(new List<RouteStepDto>(), 0, 0, false, $"Route optimization failed: {status}");
            }

            var route = root.GetProperty("routes")[0];
            var waypointOrder = route.GetProperty("waypoint_order").EnumerateArray().Select(x => x.GetInt32()).ToList();
            var legs = route.GetProperty("legs").EnumerateArray().ToList();

            var optimizedRoute = new List<RouteStepDto>();
            double totalDistance = 0;
            int totalDuration = 0;

            // Ajouter le point de départ
            optimizedRoute.Add(new RouteStepDto(0, request.Origin, request.Origin.Label, 0, 0));

            // Ajouter les waypoints optimisés
            for (int i = 0; i < waypointOrder.Count; i++)
            {
                var originalIndex = waypointOrder[i];
                var waypoint = request.Waypoints[originalIndex];
                var leg = legs[i];

                var distanceKm = leg.GetProperty("distance").GetProperty("value").GetDouble() / 1000;
                var durationMin = leg.GetProperty("duration").GetProperty("value").GetInt32() / 60;

                totalDistance += distanceKm;
                totalDuration += durationMin;

                optimizedRoute.Add(new RouteStepDto(
                    i + 1,
                    waypoint,
                    waypoint.Label,
                    Math.Round(distanceKm, 2),
                    durationMin
                ));
            }

            // Ajouter la dernière étape (vers destination)
            if (legs.Count > waypointOrder.Count)
            {
                var lastLeg = legs[^1];
                var distanceKm = lastLeg.GetProperty("distance").GetProperty("value").GetDouble() / 1000;
                var durationMin = lastLeg.GetProperty("duration").GetProperty("value").GetInt32() / 60;

                totalDistance += distanceKm;
                totalDuration += durationMin;

                var dest = request.Destination ?? request.Origin;
                optimizedRoute.Add(new RouteStepDto(
                    optimizedRoute.Count,
                    dest,
                    dest.Label ?? "Destination",
                    Math.Round(distanceKm, 2),
                    durationMin
                ));
            }

            return new RouteOptimizationResultDto(
                optimizedRoute,
                Math.Round(totalDistance, 2),
                totalDuration,
                true
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error optimizing route");
            return new RouteOptimizationResultDto(new List<RouteStepDto>(), 0, 0, false, ex.Message);
        }
    }

    public async Task<(double DistanceKm, int DurationMinutes)> GetDistanceAndDurationAsync(
        CoordinatesDto origin,
        CoordinatesDto destination)
    {
        var apiKey = _configuration["GoogleMaps:ApiKey"];

        if (string.IsNullOrEmpty(apiKey))
        {
            // Calcul approximatif sans API (formule de Haversine)
            return (CalculateHaversineDistance(origin, destination), 0);
        }

        try
        {
            var url = $"https://maps.googleapis.com/maps/api/distancematrix/json?" +
                      $"origins={origin.Latitude},{origin.Longitude}" +
                      $"&destinations={destination.Latitude},{destination.Longitude}" +
                      $"&key={apiKey}";

            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            using var document = JsonDocument.Parse(content);
            var root = document.RootElement;

            var element = root.GetProperty("rows")[0].GetProperty("elements")[0];
            var distanceKm = element.GetProperty("distance").GetProperty("value").GetDouble() / 1000;
            var durationMin = element.GetProperty("duration").GetProperty("value").GetInt32() / 60;

            return (Math.Round(distanceKm, 2), durationMin);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting distance and duration");
            return (CalculateHaversineDistance(origin, destination), 0);
        }
    }

    private static double CalculateHaversineDistance(CoordinatesDto origin, CoordinatesDto destination)
    {
        const double R = 6371; // Rayon de la Terre en km

        var lat1 = origin.Latitude * Math.PI / 180;
        var lat2 = destination.Latitude * Math.PI / 180;
        var deltaLat = (destination.Latitude - origin.Latitude) * Math.PI / 180;
        var deltaLon = (destination.Longitude - origin.Longitude) * Math.PI / 180;

        var a = Math.Sin(deltaLat / 2) * Math.Sin(deltaLat / 2) +
                Math.Cos(lat1) * Math.Cos(lat2) *
                Math.Sin(deltaLon / 2) * Math.Sin(deltaLon / 2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

        return Math.Round(R * c, 2);
    }
}
