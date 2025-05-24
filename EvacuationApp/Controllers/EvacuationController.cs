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
        private readonly IDatabase _redisCache;
        private readonly ILogger<EvacuationController> _logger;
        public EvacuationController(IDatabase radisCache, ILogger<EvacuationController> logger)
        {
            _redisCache = radisCache;
            _logger = logger;
        }

        //api/evacuation-zones
        [HttpPost("/api/evacuation-zones")]
        public IActionResult Zones([FromBody]List<ReqZoneDto> reqDatas)
        {
            //Validate&StepFlow
            //#1 : validate reqDatas
            //#2 : loop for add reqDatas to redis
            //#3 : add data zones to radis(type:list)

            try
            {
                //#1 : validate reqDatas
                if (reqDatas.Count == 0) throw new ArgumentException("Please specify the evacuation zone.");

                //#2 : loop for add reqDatas to redis
                foreach (var data in reqDatas) {
                    string jsonZone = JsonSerializer.Serialize(data);

                    //#3 : add data zones to radis(type:list)
                    _redisCache.ListRightPush("zone", jsonZone);
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
            //Validate&StepFlow
            //#1 : validate reqDatas
            //#2 : loop for add reqDatas to redis
            //#3 : add data zones to radis(type:list)

            try
            {
                //#1 : validate reqDatas
                if (reqDatas.Count == 0) throw new ArgumentException("Please specify the Vehicles");

                //#2 : loop for add reqDatas to redis
                foreach (var data in reqDatas)
                {
                    string jsonVehicle = JsonSerializer.Serialize(data);

                    //#3 : add data zones to radis(type:list)
                    _redisCache.ListRightPush("vehicle", jsonVehicle);
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
            //Validate&StepFlow
            //#1 : set variable
            //#2 : get cache data (zone/vehicle)
            //#3 : deserialize cache data (zone/vehicle)
            //#4 : loop zone for cal plan
            //#5 : get cache Existing Plan for check VehicleID 
            //#6 : loop vehicle for cal distance&time
            //#7 : Distance Calculation
            //#8 : Time Calculation
            //#9 : select plan to zone
            //#10 : add data plan to radis(type:list)

            try
            {

                //#1 : set variable
                List<ZoneDto> zones = new List<ZoneDto>();
                List<VehicleDto> vehicles = new List<VehicleDto>();
                List<PlanDto> plans = new List<PlanDto>();
                //resp dto
                List<RespPlanDto> reqPlans = new List<RespPlanDto>();

                //#2 : get cache data (zone/vehicle)
                var zoneTask = _redisCache.ListRangeAsync("zone");
                var vehicleTask = _redisCache.ListRangeAsync("vehicle");

                await Task.WhenAll(zoneTask, vehicleTask);

                var zoneCache = zoneTask.Result;
                var vehicleCache = vehicleTask.Result;

                //#3 : deserialize cache data (zone/vehicle)
                if (zoneCache.Length > 0)
                    zones = zoneCache.Select(s => JsonSerializer.Deserialize<ZoneDto>(s)).OrderByDescending(o => o.UrgencyLevel).ToList();
                
                if(vehicleCache.Length > 0)
                    vehicles = vehicleCache.Select(s => JsonSerializer.Deserialize<VehicleDto>(s)).OrderByDescending(o => o.Capacity).ToList();
                
                //#4 : loop zone for cal plan
                foreach (ZoneDto zone in zones)
                {
                    plans = new List<PlanDto>();

                    //#5 : get cache Existing Plan for check VehicleID 
                    RedisValue[] planCache = _redisCache.ListRange("plan");
                    string[] planVehicle = new string[0];
                    if (planCache.Length > 0)
                    {
                        List<PlanDto> planExisting = planCache.Select(s => JsonSerializer.Deserialize<PlanDto>(s)).ToList();
                        planVehicle = planExisting.Select(s => s.VehicleID).ToArray();
                    }

                    //#6 : loop vehicle for cal distance&time
                    foreach (VehicleDto vehicle in vehicles)
                    {
                        //#7 : Distance Calculation
                        double distance = CalDistance(zone.LocationCoordinates.Latitude
                            , zone.LocationCoordinates.Longitude
                            , vehicle.LocationCoordinates.Latitude
                            , vehicle.LocationCoordinates.Longitude);

                        //#8 : Time Calculation
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

                    //#9 : select plan to zone
                    PlanDto? plan = plans.Where(w => zone.NumberOfPeople > w.Capacity && !planVehicle.Contains(w.VehicleID))
                                        .OrderBy(o => o.Distance)
                                        .ThenBy(t => t.ETA)
                                        .FirstOrDefault();
                    if(plan != null)
                    {
                        //#10 : add data plan to radis(type:list)
                        string jsonPlan = JsonSerializer.Serialize(plan);
                        _redisCache.ListRightPush("plan", jsonPlan);

                        reqPlans.Add(new RespPlanDto()
                        {
                            ZoneID = plan.ZoneID,
                            VehicleID = plan.VehicleID,
                            Distance = plan.Distance,
                            ETA = $"{plan.ETA} minutes",
                            NumberOfPeople = plan.Capacity,
                        });
                    }
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
            //Validate&StepFlow
            //#1 : set variable
            //#2 : get cache data (zone/plan)
            //#3 : deserialize cache data (zone/plan)
            //#4 : get cache status by ZoneID
            //#5 : loop zone for cal totalEvacuated/RemainingPeople

            try
            {
                //#1 : set variable
                List<ZoneDto> zones = new List<ZoneDto>();
                List<PlanDto> plans = new List<PlanDto>();
                StatusDto[] statuses = null;
                //resp dto
                List<RespStatusDto> respStatus = new List<RespStatusDto>();

                //#2 : get cache data (zone/plan)
                var zoneTask = _redisCache.ListRangeAsync("zone");
                var planTask = _redisCache.ListRangeAsync("plan");

                await Task.WhenAll(zoneTask, planTask);

                var zoneCache = zoneTask.Result;
                var planCache = planTask.Result;

                //#3 : deserialize cache data (zone/plan)
                if (zoneCache.Length > 0)
                    zones = zoneCache.Select(s => JsonSerializer.Deserialize<ZoneDto>(s)).ToList();

                if (planCache.Length > 0)
                    plans = planCache.Select(s => JsonSerializer.Deserialize<PlanDto>(s)).ToList();

                //#4 : get cache status by ZoneID
                RedisKey[] zoneKey = zones.Select(s => (RedisKey)$"status:{s.ZoneID}").ToArray();
                var statusCache = _redisCache.StringGet(zoneKey).Where(s => s.HasValue).ToArray();
                if (statusCache.Length > 0)
                    statuses = statusCache.Select(s => JsonSerializer.Deserialize<StatusDto>(s)).ToArray();

                //#5 : loop zone for cal totalEvacuated/RemainingPeople
                foreach (ZoneDto zone in zones) 
                {
                    int totalEvacuated = 0;
                    int RemainingPeople = zone.NumberOfPeople;
                    if(statuses != null)
                    {
                        StatusDto? status = statuses.FirstOrDefault(w => w.ZoneID == zone.ZoneID);
                        if (status != null)
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
        public async Task<IActionResult> updateAsync([FromBody]ReqUpdateDto reqData)
        {
            //Validate&StepFlow
            //#1 : set variable
            //#2 : get cache data (zone/status)
            //#3 : deserialize cache data (zone/status)
            //#4 : cal total evacuated
            //#5 : cal remaining people
            //#6 : update status to radis
            //#7 : add to log 

            try
            {
                //#1 : set variable
                ZoneDto? zone = new ZoneDto();
                PlanDto? plan = new PlanDto();
                StatusDto? status = new StatusDto();

                //#2 : get cache data (zone/plan/status)
                var zoneTask = _redisCache.ListRangeAsync("zone");
                var planTask = _redisCache.ListRangeAsync("plan");
                var statusTask = _redisCache.StringGetAsync($"status:{reqData.ZoneID}");

                await Task.WhenAll(zoneTask, planTask, statusTask);

                var zoneCache = zoneTask.Result;
                var planCache = planTask.Result;
                var statusCache = statusTask.Result;

                //#3 : deserialize cache data (zone/status)
                if (zoneCache.Length > 0)
                    zone = zoneCache.Select(s => JsonSerializer.Deserialize<ZoneDto>(s)).FirstOrDefault(f => f?.ZoneID == reqData.ZoneID);

                if (planCache.Length > 0)
                    plan = planCache.Select(s => JsonSerializer.Deserialize<PlanDto>(s)).FirstOrDefault(f => f?.ZoneID == reqData.ZoneID);

                if (statusCache.HasValue)
                    status = JsonSerializer.Deserialize<StatusDto>(statusCache);


                if (zone != null)
                {

                    //#4 : cal total evacuated
                    int newTotalEvacuated = status.TotalEvacuated + reqData.TotalEvacuated;

                    //#5 : cal remaining people
                    int newRemainingPeople = zone.NumberOfPeople - newTotalEvacuated;

                    status = new StatusDto()
                    {
                        ZoneID = zone.ZoneID,
                        TotalEvacuated = newTotalEvacuated,
                        RemainingPeople = newRemainingPeople,
                        LastVehicleUsed = reqData.VehicleID
                    };

                    //#6 : update status to radis
                    string statusJson = JsonSerializer.Serialize(status);
                    _redisCache.StringSet($"status:{zone.ZoneID}", statusJson);

                    //#7 : add to log 
                    var statusToLogger = new
                    {
                        ZoneID = zone.ZoneID,
                        TotalEvacuated = newTotalEvacuated,
                        RemainingPeople = newRemainingPeople,
                        LastVehicleUsed = reqData.VehicleID,
                        ETA = plan.ETA,
                        EvacuationStatus = newRemainingPeople == 0 ? "complete" : "incomplete"
                    };

                    _logger.LogInformation(JsonSerializer.Serialize(statusToLogger));
                }


                return Ok(status);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
            
        }

        //api/evacuations/clear
        [HttpDelete("clear")]
        public async Task<IActionResult> ClearAsync()
        {
            //Validate&StepFlow
            //#1 : set variable
            //#2 : get cache data and deserialize (plans) 
            //#3 : get cache data and deserialize (statuses) 
            //#4 : loop for check status at zone complete 
            //#5 : check RemainingPeople
            //#6 : task clear status
            //#7 : task clear plan
            //#8 : run task for clear data in redis

            try
            {
                //#1 : set variable
                List<PlanDto> plans = new List<PlanDto>();
                StatusDto[] statuses = new StatusDto[0];
                var tasks = new List<Task>();

                //#2 : get cache data and deserialize (plans) 
                var planCache = _redisCache.ListRange("plan");
                if (planCache.Length > 0)
                    plans = planCache.Select(s => JsonSerializer.Deserialize<PlanDto>(s)).ToList();

                //#3 : get cache data and deserialize (statuses) 
                RedisKey[] planZone = plans.Select(s => (RedisKey)$"status:{s.ZoneID}").ToArray();
                var statusCache = _redisCache.StringGet(planZone).Where(s => s.HasValue).ToArray();
                if (statusCache.Length > 0)
                    statuses = statusCache.Select(s => JsonSerializer.Deserialize<StatusDto>(s)).ToArray();

                //#4 : loop for check status at zone complete 
                foreach (var status in statuses)
                {
                    //#5 : check RemainingPeople
                    if (status.RemainingPeople <= 0)
                    {
                        //#6 : task clear status
                        Task<bool> statusCacheClear = _redisCache.KeyDeleteAsync($"status:{status.ZoneID}");
                        tasks.Add(statusCacheClear);

                        //#7 : task clear plan
                        PlanDto plan = plans.FirstOrDefault(f => f.ZoneID == status.ZoneID);
                        string planJson = JsonSerializer.Serialize(plan);
                        Task<long> planCacheClear = _redisCache.ListRemoveAsync("plan", planJson);
                        tasks.Add(planCacheClear);

                    }
                }

                //#8 : run task for clear data in redis
                await Task.WhenAll(tasks);

                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
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
