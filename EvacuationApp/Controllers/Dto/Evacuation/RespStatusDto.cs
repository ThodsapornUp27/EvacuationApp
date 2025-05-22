namespace EvacuationApp.Controllers.Dto.Evacuation
{
    public class RespStatusDto
    {
        public string ZoneID { get; set; }
        public int TotalEvacuated { get; set; }
        public int RemainingPeople { get; set; }
        public string LastVehicleUsed { get; set; }
    }
}
