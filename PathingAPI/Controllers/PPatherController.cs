using PathingAPI.WorldToMap;
using Microsoft.AspNetCore.Mvc;
using PatherPath.Graph;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace PathingAPI.Controllers
{
    public class SearchParameters
    {
        public int FromUIMapId { get; set; }
        public float FromV1 { get; set; }
        public float FromV2 { get; set; }
        public int ToUIMapId { get; set; }
        public float ToV1 { get; set; }
        public float ToV2 { get; set; }
    }

    [Route("api/[controller]")]
    [ApiController]
    public class PPatherController : ControllerBase
    {
        PPatherService service;

        public PPatherController(PPatherService service)
        {
            this.service = service;
        }

        /// <summary>
        /// Allows a route to be calculated from one point to another using minimap coords.
        /// </summary>
        /// <remarks>
        /// map1 and map2 are the map ids. See https://wow.gamepedia.com/API_C_Map.GetBestMapForUnit
        ///  
        ///     /dump C_Map.GetBestMapForUnit("player")
        ///     
        ///     Dump: value=_Map.GetBestMapForUnit("player")
        ///     [1]=1451
        ///     
        /// x and y are the map coordinates for the zone (same as the mini map). See https://wowwiki.fandom.com/wiki/API_GetPlayerMapPosition
        /// 
        ///     local posX, posY = GetPlayerMapPosition("player");
        /// </remarks>
        /// <param name="map1">from map e.g. 1451</param>
        /// <param name="x1">from X e.g. 46.8</param>
        /// <param name="y1">from Y e.g. 54.2</param>
        /// <param name="map2">to map e.g. 1451</param>
        /// <param name="x2">to X e.g. 51.2</param>
        /// <param name="y2">to Y e.g. 38.9</param>
        /// <returns>A list of x,y,z and mapid</returns>
        [HttpGet("MapRoute")]
        [Produces("application/json")]
        public JsonResult MapRoute(int map1, float x1, float y1, int map2, float x2, float y2)
        {
            service.SetLocations(service.GetWorldLocation(map1, x1, y1), service.GetWorldLocation(map2, x2, y2));
            var path = service.DoSearch(PatherPath.Graph.PathGraph.eSearchScoreSpot.A_Star);

            //return new JsonResult(path.locations, new JsonSerializerOptions
            //{
            //    WriteIndented=true
            //});

            var worldLocations = path == null ? new List<WorldMapAreaSpot>() : path.locations.Select(s => service.ToMapAreaSpot(s.X, s.Y, s.Z, map1));

            return new JsonResult(worldLocations);
            //return Newtonsoft.Json.JsonConvert.SerializeObject(worldLocations);
        }

        /// <summary>
        /// Allows a route to be calculated from one point to another using world coords.
        /// e.g. -896, -3770, 11, (Barrens,Rachet) to -441, -2596, 96, (Barrens,Crossroads,Barrens)
        /// </summary>
        /// <param name="x1">from X e.g. -896</param>
        /// <param name="y1">from Y e.g. -3770</param>
        /// <param name="z1">from Y e.g. 11</param>
        /// <param name="x2">to X e.g. -441</param>
        /// <param name="y2">to Y e.g. -2596</param>
        /// <param name="z2">from Y e.g. 96</param>
        /// <param name="continent">from ["Azeroth", "Kalimdor", "Northrend", "Expansion01"] e.g. Kalimdor</param>
        /// <returns>A list of x,y,z</returns>
        [HttpGet("WorldRoute")]
        [Produces("application/json")]
        public string WorldRoute(float x1, float y1, float z1, float x2, float y2, float z2, string continent)
        {
            service.SetLocations(new Location(x1,y1,z1,"l1",continent), new Location(x2, y2, z2, "l2", continent));
            var path = service.DoSearch(PatherPath.Graph.PathGraph.eSearchScoreSpot.A_Star);
            return Newtonsoft.Json.JsonConvert.SerializeObject(path);
        }
    }
}