namespace EvacuationApp.Controllers.Dto.Evacuation
{
    public class ReqVehicleDto
    {
        public string VehicleID { get; set; }
        public int Capacity { get; set; }
        public string Type { get; set; }
        public ReqLocationCoordinatesDto LocationCoordinates { get; set; }
        public int Speed { get; set; }
        public ReqVehicleDto() { }
    }
}
