using System;
using System.Collections.Generic;

namespace DashRL.Models;

public class CustomOverlay
{
    public int Version { get; set; } = 1;

    public string Id { get; set; } =
        Guid.NewGuid().ToString("N");

    public string Name { get; set; } =
        "New Overlay";

    public double Width { get; set; } = 500;

    public double Height { get; set; } = 150;

    public List<CustomOverlayElement> Elements { get; set; } = new();
}