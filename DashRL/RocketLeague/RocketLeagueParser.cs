using System;
using System.Text.Json;
using DashRL.Models;

namespace DashRL.RocketLeague;

public static class RocketLeagueParser
{
    public static MatchState? ParseMatchState(string message)
    {
        try
        {
            // Premier niveau du JSON :
            // { "Event": "UpdateState", "Data": "{...}" }

            using JsonDocument document = JsonDocument.Parse(message);

            JsonElement root = document.RootElement;

            if (!root.TryGetProperty("Event", out JsonElement eventElement))
                return null;

            string? eventName = eventElement.GetString();

            // Pour l'instant, seul UpdateState nous intéresse.
            if (eventName != "UpdateState")
                return null;

            if (!root.TryGetProperty("Data", out JsonElement dataElement))
                return null;

            // Data est lui-même une chaîne contenant du JSON.
            string? dataJson = dataElement.GetString();

            if (string.IsNullOrWhiteSpace(dataJson))
                return null;

            // Deuxième désérialisation :
            // JSON contenu dans Data → MatchState
            return JsonSerializer.Deserialize<MatchState>(
                dataJson,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }
            );
        }
        catch (JsonException ex)
        {
            Console.WriteLine(
                $"[DashRL] Erreur JSON : {ex.Message}"
            );

            return null;
        }
    }
}