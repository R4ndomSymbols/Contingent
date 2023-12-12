using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using StudentTracking.Models;
using Utilities.Validation;

namespace StudentTracking.Controllers;


public class SpecialityController : Controller{

    private readonly ILogger<SpecialityController> _logger;

    public SpecialityController(ILogger<SpecialityController> logger)
    {
        _logger = logger;
    }
 
    [HttpGet]
    [Route("specialities/modify/{query}")]
    public IActionResult ProcessSpeciality(string query){
        if (query == "new"){
            return View(@"Views/Modify/SpecialityModify.cshtml", new SpecialityModel()); 
        }
        else if(int.TryParse(query, out int id)){
            var got = SpecialityModel.GetById(id, null);
            if (got!=null){
                return View(@"Views/Modify/SpecialityModify.cshtml", got);
            }
            else{
                return View(@"Views/Shared/Error.cshtml", "Специальности с таким id не существует");
            }
            
        }
        else{
            return View(@"Views/Shared/Error.cshtml", "Недопустимый id специальности");
        }
    }
    [HttpPost]
    [Route("specialities/add")]
    public async Task<JsonResult> AddOrUpdateSpeciality(){
        using(var reader = new StreamReader(Request.Body)){
            var body = await reader.ReadToEndAsync();
            var deserialized = JsonSerializer.Deserialize<SpecialityModel>(body);
            if (deserialized!=null){
                await deserialized.Save(null);
                Console.WriteLine(await deserialized.GetCurrentState(null));
                if (await deserialized.GetCurrentState(null) == RelationTypes.Bound){
                    return Json(new { deserialized.Id});
                }
                else {
                    return Json(deserialized.GetErrors());
                }
            }
            return Json(new object());
        }    
    }
    [HttpGet]
    [Route("specialities/suggest/{query?}")]
    public async Task<JsonResult> GetSuggestions(string? query){
        return Json(await SpecialityModel.GetSuggestions(query, null));
    }

}