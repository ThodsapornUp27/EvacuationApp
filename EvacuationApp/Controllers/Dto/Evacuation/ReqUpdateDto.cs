namespace EvacuationApp.Controllers.Dto.Evacuation
{
    public class ReqUpdateDto
    {
        public string ZoneID { get; set; }
        public string VehicleID { get; set; }
        public int TotalEvacuated { get; set; }
    }
}
