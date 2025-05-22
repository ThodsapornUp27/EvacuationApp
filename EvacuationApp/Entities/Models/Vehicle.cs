using System;
using System.Collections.Generic;

namespace EvacuationApp.Entities.Models;

public partial class Vehicle
{
    public int VehicleId { get; set; }

    public int? Capacity { get; set; }

    public string? Type { get; set; }

    public double? Latitude { get; set; }

    public double? Longitude { get; set; }

    public double? Speed { get; set; }
}
