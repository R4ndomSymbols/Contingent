using System.Diagnostics;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using StudentTracking.Models;

namespace StudentTracking.Controllers;

public class AddressController : Controller
{
    private readonly ILogger<AddressController> _logger;

    public AddressController(ILogger<AddressController> logger)
    {
        _logger = logger;
    }
    [HttpGet]
    [Route("/addresses/federal")]
    public JsonResult GetAllCountryRegions(){
        var dbResponce = AddressModel.GetAllFederalRegions();
        if (dbResponce!=null){
            return Json(dbResponce.ToArray());
        }
        return Json(null);   
    }
    [HttpGet]
    [Route("/addresses/regions/{parentId}")]
    public JsonResult GetRegions(string parentId){
        if (int.TryParse(parentId, out int realId))
        {
            var dbResponce = AddressModel.GetRegionsWithin(realId);
            if (dbResponce!=null){
                return Json(dbResponce.ToArray());
            }
        }
        return Json(null);   
    }
    [HttpGet]
    [Route("/addresses/localities/{parentId}")]
    public JsonResult GetLocalities(string parentId){
        if (int.TryParse(parentId, out int realId))
        {
            var dbResponce = AddressModel.GetLocalitiesWithin(realId);
            if (dbResponce!=null){
                return Json(dbResponce.ToArray());
            }
        }
        return Json(null);   
    }
    [HttpGet]
    [Route("/addresses/streets/{parentId}")]
    public JsonResult GetStreets(string parentId){
        if (int.TryParse(parentId, out int realId))
        {
            var dbResponce = AddressModel.GetStreetsWithin(realId);
            if (dbResponce!=null){
                return Json(dbResponce.ToArray());
            }
        }
        return Json(null);   
    }
    [HttpGet]
    [Route("/addresses/buildings/{parentId}")]
    public JsonResult GetBuildings(string parentId){
        if (int.TryParse(parentId, out int realId))
        {
            var dbResponce = AddressModel.GetBuildingsOn(realId);
            if (dbResponce!=null){
                return Json(dbResponce.ToArray());
            }
        }
        return Json(null);   
    }
    [HttpPost]
    [Route("/addresses/new")]
    public async Task<JsonResult> CreateAddress(){
        using(var reader = new StreamReader(Request.Body)){
            var body = await reader.ReadToEndAsync();
            var parsed = JsonSerializer.Deserialize<AddressModelPrototype>(body);
            return Json(new {
                addressId = AddressModel.GetAddressId(parsed)
                });
        }   
    }
    [HttpPost]
    [Route("/addresses/new/federal")]
    public async Task<IActionResult> CreateCountryRegion(){
        using(var reader = new StreamReader(Request.Body)){
            var body = await reader.ReadToEndAsync();
            var parsed = JsonSerializer.Deserialize<CountryRegion>(body);
            Console.WriteLine(parsed.Id + " " + parsed.FullName);
            AddressModel.SaveCountryRegion(parsed);
            return Ok();
        }   
    }
    [HttpPost]
    [Route("/addresses/new/region")]
    public async Task<JsonResult> CreateRegion(){
        using(var reader = new StreamReader(Request.Body)){
            var body = await reader.ReadToEndAsync();
            var parsed = JsonSerializer.Deserialize<Region>(body);
            return Json(new {id = AddressModel.SaveRegion(parsed)});
        }   
    }
    [HttpPost]
    [Route("/addresses/new/locality")]
    public async Task<JsonResult> CreateLocality(){
        using(var reader = new StreamReader(Request.Body)){
            var body = await reader.ReadToEndAsync();
            var parsed = JsonSerializer.Deserialize<Locality>(body);
            return Json(new {id = AddressModel.SaveLocality(parsed)});
        }   
    }
    [HttpPost]
    [Route("/addresses/new/street")]
    public async Task<JsonResult> CreateStreet(){
        using(var reader = new StreamReader(Request.Body)){
            var body = await reader.ReadToEndAsync();
            var parsed = JsonSerializer.Deserialize<Street>(body);
            return Json(new {id = AddressModel.SaveStreet(parsed)});
        }   
    }
    [HttpPost]
    [Route("/addresses/new/building")]
    public async Task<JsonResult> CreateBuilding(){
        using(var reader = new StreamReader(Request.Body)){
            var body = await reader.ReadToEndAsync();
            var parsed = JsonSerializer.Deserialize<Building>(body);
            return Json(new {id = AddressModel.SaveBuilding(parsed)});
        }   
    }


    public IActionResult Index()
    {
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}