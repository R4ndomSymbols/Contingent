using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using StudentTracking.Controllers.DTO.In;
using StudentTracking.Controllers.DTO.Out;
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
            return View(@"Views/Modify/SpecialityModify.cshtml", new SpecialityOutDTO()); 
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
    public async Task<IActionResult> AddOrUpdateSpeciality(){
        using var reader = new StreamReader(Request.Body);
        var body = await reader.ReadToEndAsync();
        SpecialityDTO dto;
        try{
            dto = JsonSerializer.Deserialize<SpecialityDTO>(body);
        }
        catch (Exception){
            return BadRequest("Неверный формат данных");
        }

        if (dto!=null){
            var result = SpecialityModel.Build(dto);
            if (result.IsFailure){
                return Json(new ErrorsDTO(result.Errors));
            }
            else {
                await result.ResultObject.Save();
                return Json(new SpecialityOutDTO(result.ResultObject));
            }
        }
        return BadRequest("Неверный формат данных");    
    }

    [HttpGet]
    [Route("specialities/suggest/{query?}")]
    public async Task<JsonResult> GetSuggestions(string? query){
        return Json(await SpecialityModel.GetSuggestions(query, null));
    }

}