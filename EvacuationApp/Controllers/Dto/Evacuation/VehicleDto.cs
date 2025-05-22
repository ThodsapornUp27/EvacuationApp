namespace EvacuationApp.Controllers.Dto.Evacuation
{
    public class VehicleDto
    {
        public string VehicleID { get; set; }
        public int Capacity { get; set; }
        public string Type { get; set; }
        public LocationCoordinatesDto LocationCoordinates { get; set; }
        public int Speed { get; set; }
        public VehicleDto() { }
    }
}
