namespace EvacuationApp.Controllers.Dto.Evacuation
{
    public class PlanDto
    {
        public string ZoneID { get; set; }
        public string VehicleID { get; set; }
        public double Distance { get; set; }
        public string ETA { get; set; }
        public int Capacity { get; set; }
    }
}
