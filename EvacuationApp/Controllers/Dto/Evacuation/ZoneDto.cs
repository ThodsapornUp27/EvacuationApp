namespace EvacuationApp.Controllers.Dto.Evacuation
{
    public class ZoneDto
    {
        public string ZoneID { get; set; }
        public LocationCoordinatesDto LocationCoordinates { get; set; }
        public int NumberOfPeople { get; set; }
        public int UrgencyLevel { get; set; }
        public ZoneDto() { }
    }
}
