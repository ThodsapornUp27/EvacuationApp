using System.Text.Json;
using EvacuationApp.Controllers.Dto.Evacuation;
using Microsoft.AspNetCore.Mvc;
using StackExchange.Redis;

namespace EvacuationApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EvacuationController : ControllerBase
    {
        private readonly IDatabase _radisCache;
        public EvacuationController(IDatabase radisCache) 
        {
            _radisCache = radisCache;
        }

        //api/evacuation-zones
        [HttpPost("/api/evacuation-zones")]
        public IActionResult Zones([FromBody]List<ReqZoneDto> reqDatas)
        {
            try
            {
                foreach (var data in reqDatas) {
                    string jsonZone = JsonSerializer.Serialize(data);

                    //set data zones to radis(type:list)
                    _radisCache.ListRightPush("zone", jsonZone);
                }


                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }

        }

        //api/vehicles
        [HttpPost("/api/vehicles")]
        public IActionResult Vehicles([FromBody]List<ReqVehicleDto> reqDatas)
        {
            try
            {
                foreach (var data in reqDatas)
                {
                    string jsonVehicle = JsonSerializer.Serialize(data);

                    //set data vehicle to radis(type:list)
                    _radisCache.ListRightPush("vehicle", jsonVehicle);
                }

                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        //api/evacuations/plan
        [HttpPost("plan")]
        public async Task<IActionResult> PlanAsync()
        {
            try
            {
                List<ZoneDto> zones = new List<ZoneDto>();
                List<VehicleDto> vehicles = new List<VehicleDto>();
                List<PlanDto> plans = new List<PlanDto>();

                //resp dto
                List<RespPlanDto> reqPlans = new List<RespPlanDto>();

                //get cache data  
                var zoneTask = _radisCache.ListRangeAsync("zone");
                var vehicleTask = _radisCache.ListRangeAsync("vehicle");

                await Task.WhenAll(zoneTask, vehicleTask);

                var zoneCache = zoneTask.Result;
                var vehicleCache = vehicleTask.Result;

                //deserialize cache data
                if(zoneCache.Length > 0)
                    zones = zoneCache.Select(s => JsonSerializer.Deserialize<ZoneDto>(s)).OrderByDescending(o => o.UrgencyLevel).ToList();
                
                if(vehicleCache.Length > 0)
                    vehicles = vehicleCache.Select(s => JsonSerializer.Deserialize<VehicleDto>(s)).OrderByDescending(o => o.Capacity).ToList();
                

                foreach (ZoneDto zone in zones)
                {
                    plans = new List<PlanDto>();

                    //get cache Existing Plan 
                    RedisValue[] planCache = _radisCache.ListRange("plan");
                    string[] planVehicle = new string[0];
                    if (planCache.Length > 0)
                    {
                        List<PlanDto> planExisting = planCache.Select(s => JsonSerializer.Deserialize<PlanDto>(s)).ToList();
                        planVehicle = planExisting.Select(s => s.VehicleID).ToArray();
                    }

                    foreach (VehicleDto vehicle in vehicles)
                    {
                        //Distance Calculation
                        double distance = CalDistance(zone.LocationCoordinates.Latitude
                            , zone.LocationCoordinates.Longitude
                            , vehicle.LocationCoordinates.Latitude
                            , vehicle.LocationCoordinates.Longitude);

                        //Time Calculation
                        double time = CalTime(distance, vehicle.Speed);

                        plans.Add(new PlanDto()
                        {
                            ZoneID = zone.ZoneID,
                            VehicleID = vehicle.VehicleID,
                            Distance = distance,
                            ETA = $"{time} minutes",
                            Capacity = vehicle.Capacity,
                        });
                    }

                    PlanDto? plan = plans.Where(w => zone.NumberOfPeople > w.Capacity && !planVehicle.Contains(w.VehicleID))
                                        .OrderBy(o => o.Distance)
                                        .ThenBy(t => t.ETA)
                                        .FirstOrDefault();

                    string jsonPlan = JsonSerializer.Serialize(plan);
                    //set data plan to radis(type:list)
                    _radisCache.ListRightPush("plan", jsonPlan);

                    reqPlans.Add(new RespPlanDto()
                    {
                        ZoneID = plan.ZoneID,
                        VehicleID = plan.VehicleID,
                        Distance = plan.Distance,
                        ETA = $"{plan.ETA} minutes",
                        NumberOfPeople = plan.Capacity,
                    });
                }

                return Ok(reqPlans);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        //api/evacuations/status
        [HttpGet("status")]
        public async Task<IActionResult> StatusAsync()
        {
            try
            {
                List<ZoneDto> zones = new List<ZoneDto>();
                List<PlanDto> plans = new List<PlanDto>();
                List<StatusDto> statuses = new List<StatusDto>();

                //resp dto
                List<RespStatusDto> respStatus = new List<RespStatusDto>();

                //get cache data  
                var zoneTask = _radisCache.ListRangeAsync("zone");
                var planTask = _radisCache.ListRangeAsync("plan");
                var statusTask = _radisCache.ListRangeAsync("status");

                await Task.WhenAll(zoneTask, planTask, statusTask);

                var zoneCache = zoneTask.Result;
                var planCache = planTask.Result;
                var statusCache = statusTask.Result;

                //deserialize cache data
                if (zoneCache.Length > 0)
                    zones = zoneCache.Select(s => JsonSerializer.Deserialize<ZoneDto>(s)).ToList();

                
                if (planCache.Length > 0)
                    plans = planCache.Select(s => JsonSerializer.Deserialize<PlanDto>(s)).ToList();

                
                if (statusCache.Length > 0)
                    statuses = statusCache.Select(s => JsonSerializer.Deserialize<StatusDto>(s)).ToList();
                    

                foreach (ZoneDto zone in zones) 
                {
                    int totalEvacuated = 0;
                    int RemainingPeople = zone.NumberOfPeople;
                    StatusDto? status = statuses.FirstOrDefault(w => w.ZoneID == zone.ZoneID);
                    if (status != null)
                    {
                        totalEvacuated = status.TotalEvacuated;
                        RemainingPeople = zone.NumberOfPeople - totalEvacuated;
                    }

                    respStatus.Add(new RespStatusDto{
                        ZoneID = zone.ZoneID,
                        TotalEvacuated = totalEvacuated,
                        RemainingPeople = RemainingPeople,
                    });
                }

                return Ok(respStatus);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }

            
        }

        //api/evacuations/update
        [HttpPut("update")]
        public IActionResult update()
        {
            try
            {

            }
            catch (Exception ex)
            {

            }

            return Ok();
        }

        //api/evacuations/clear
        [HttpDelete("clear")]
        public IActionResult clear()
        {
            try
            {

            }
            catch (Exception ex)
            {

            }

            return Ok();
        }

        //ref https://www.geeksforgeeks.org/haversine-formula-to-find-distance-between-two-points-on-a-sphere/?ref=rp
        private double CalDistance(double lat1, double lon1,double lat2, double lon2)
        {
            // distance between latitudes and longitudes
            double dLat = (Math.PI / 180) * (lat2 - lat1);
            double dLon = (Math.PI / 180) * (lon2 - lon1);

            // convert to radians
            lat1 = (Math.PI / 180) * (lat1);
            lat2 = (Math.PI / 180) * (lat2);

            // apply formulae
            double a = Math.Pow(Math.Sin(dLat / 2), 2) +
                       Math.Pow(Math.Sin(dLon / 2), 2) *
                       Math.Cos(lat1) * Math.Cos(lat2);
            double rad = 6371;
            double c = 2 * Math.Asin(Math.Sqrt(a));
            return rad * c;
        }

        private double CalTime(double dist, double speed)
        {
            return (dist / speed)*60;
        }
    }
}
