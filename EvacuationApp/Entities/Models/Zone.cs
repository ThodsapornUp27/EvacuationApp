using System;
using System.Collections.Generic;

namespace EvacuationApp.Entities.Models;

public partial class Zone
{
    public int ZonesId { get; set; }

    public double? Latitude { get; set; }

    public double? Longitude { get; set; }

    public int? PeopleNumber { get; set; }

    public int? UrgencyLevel { get; set; }
}
