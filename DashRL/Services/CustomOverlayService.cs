using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using DashRL.Models;

namespace DashRL.Services;

public class CustomOverlayService
{
    private static readonly string OverlaysDirectory =
        Path.Combine(
            Environment.GetFolderPath(
                Environment.SpecialFolder.LocalApplicationData
            ),
            "DashRL",
            "Overlays"
        );

    private static readonly JsonSerializerOptions JsonOptions =
        new()
        {
            WriteIndented = true
        };

    public CustomOverlayService()
    {
        Directory.CreateDirectory(
            OverlaysDirectory
        );
    }

    public string Save(
        CustomOverlay overlay)
    {
        Directory.CreateDirectory(
            OverlaysDirectory
        );

        string fileName =
            $"{overlay.Id}.dashoverlay";

        string path =
            Path.Combine(
                OverlaysDirectory,
                fileName
            );

        string json =
            JsonSerializer.Serialize(
                overlay,
                JsonOptions
            );

        File.WriteAllText(
            path,
            json
        );

        return path;
    }

    public CustomOverlay? Load(
        string path)
    {
        try
        {
            if (!File.Exists(path))
                return null;

            string json =
                File.ReadAllText(path);

            return JsonSerializer
                .Deserialize<CustomOverlay>(
                    json,
                    JsonOptions
                );
        }
        catch
        {
            return null;
        }
    }

    public IReadOnlyList<CustomOverlay> LoadAll()
    {
        Directory.CreateDirectory(
            OverlaysDirectory
        );

        List<CustomOverlay> overlays =
            new();

        foreach (string path in
                 Directory.EnumerateFiles(
                     OverlaysDirectory,
                     "*.dashoverlay"))
        {
            CustomOverlay? overlay =
                Load(path);

            if (overlay != null)
                overlays.Add(overlay);
        }

        return overlays
            .OrderBy(
                overlay => overlay.Name,
                StringComparer.OrdinalIgnoreCase
            )
            .ToList();
    }

    public void Delete(
        CustomOverlay overlay)
    {
        string path =
            Path.Combine(
                OverlaysDirectory,
                $"{overlay.Id}.dashoverlay"
            );

        if (File.Exists(path))
            File.Delete(path);
    }

    public CustomOverlay CreateDefault(
        string name)
    {
        CustomOverlay overlay =
            new()
            {
                Name = name
            };

        overlay.Elements.Add(
            new CustomOverlayElement
            {
                Type =
                    CustomOverlayElementType.Wins,

                X = 20,
                Y = 20,

                Width = 100,
                Height = 40,

                FontSize = 18,

                TextColor =
                    "#72D69A",

                Bold = true
            }
        );

        overlay.Elements.Add(
            new CustomOverlayElement
            {
                Type =
                    CustomOverlayElementType.Losses,

                X = 130,
                Y = 20,

                Width = 100,
                Height = 40,

                FontSize = 18,

                TextColor =
                    "#E87979",

                Bold = true
            }
        );

        overlay.Elements.Add(
            new CustomOverlayElement
            {
                Type =
                    CustomOverlayElementType.Streak,

                X = 240,
                Y = 20,

                Width = 100,
                Height = 40,

                FontSize = 18,

                TextColor =
                    "#FFFFFF",

                Bold = true
            }
        );

        return overlay;
    }
}