namespace EvacuationApp.Controllers.Dto.Evacuation
{
    public class ReqZoneDto
    {
        public string ZoneID { get; set; }
        public ReqLocationCoordinatesDto LocationCoordinates { get; set; }
        public int NumberOfPeople { get; set; }
        public int UrgencyLevel { get; set; }
        public ReqZoneDto() { }
    }
}
