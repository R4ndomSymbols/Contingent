using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using StudentTracking.Models;

namespace StudentTracking.Controllers;


public class SpecialityController : Controller{

    private readonly ILogger<SpecialityController> _logger;

    public SpecialityController(ILogger<SpecialityController> logger)
    {
        _logger = logger;
    }

    [HttpGet]
    [Route("specialities/speciality/{query}")]
    public IActionResult ProcessSpeciality(string query){
        if (query == "new"){
            return View(@"Views/Models/Speciality.cshtml", new SpecialityModel()); 
        }
        else if(int.TryParse(query, out int id)){
            var got = SpecialityModel.GetById(id);
            if (got!=null){
                return View(@"Views/Models/Speciality.cshtml", got);
            }
            else{
                return View(@"Views/Shared/Error.cshtml", "Специальности с таким id не существует");
            }
            
        }
        else{
            return View(@"Views/Shared/Error.cshtml", "Недопустимый id группы");
        }
    }
    [HttpGet]
    [Route("specialities/fgoscodes")]
    public JsonResult GetFGOSCodes(){
        
        var got = SpecialityModel.GetAllFGOSCodes();
        if (got!=null){
            return Json(got.ToArray());
        }
        return Json(null);
    }
    [HttpGet]
    [Route("specialities/fgosnames")]
    public JsonResult GetFGOSNames(){
        
        var got = SpecialityModel.GetAllFGOSNames();
        if (got!=null){
            return Json(got.ToArray());
        }
        return Json(null);
    }
    [HttpGet]
    [Route("specialities/types")]
    public JsonResult GetSpecialityTypes(){
        
        var got = SpecialityModel.GetAllTypes();
        if (got!=null){
            return Json(got.ToArray());
        }
        return Json(null);
    }
    [HttpPost]
    [Route("specialities/add")]
    public async Task<JsonResult> AddOrUpdateSpeciality(){
        using(var reader = new StreamReader(Request.Body)){
            var body = await reader.ReadToEndAsync();
            var deserialized = JsonSerializer.Deserialize<SpecialityModel>(body);
            int result = -1;
            if (deserialized!=null){
                result = SpecialityModel.CreateOrUpdateSpeciality(deserialized);
            }
            return Json(result);
        }    
    }
    [HttpGet]
    [Route("specialities/forgroups")]
    public JsonResult GetSpecialitiesForGroups(){
        var got = SpecialityModel.GetAllGroupView();
        if (got!=null){
            return Json(got.ToArray());
        }
        return Json(null);
    }
}