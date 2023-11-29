using System.Diagnostics;
using System.Runtime.Serialization.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;
using StudentTracking.Models.Domain;
using Utilities;

namespace StudentTracking.Controllers;
public class StudentController : Controller
{
    private readonly ILogger<HomeController> _logger;

    public StudentController(ILogger<HomeController> logger)
    {
        _logger = logger;
    }
    // отрисовка страницы редактирования и добавления
    [HttpGet]
    [Route("students/modify/{query}")]
    public async Task<IActionResult> ProcessStudent(string query)
    {   
        if (query == "new"){
            return View(@"Views/Modify/StudentModify.cshtml", new StudentModel());
        }
        if (int.TryParse(query, out int id)){
            StudentModel? student = await StudentModel.GetStudentById(id);
            if (student == null){
                return View(@"Views/Shared/Error.cshtml", "Такого студента не существует");
            }
            return View(@"Views/Modify/StudentModify.cshtml", student);
        }
        else{
            return View(@"Views/Shared/Error.cshtml", "Недопустимый id");
        }
        
    }
    [HttpPost]
    [Route("/students/add")]
    public async Task<JsonResult> CreateStudent(){
        using(var reader = new StreamReader(Request.Body)){
            var body = await reader.ReadToEndAsync();
            var settings = new JsonSerializerOptions();
            //try{
                var deserialized = JsonSerializer.Deserialize<StudentModel>(body);
                if (deserialized!=null)
                {
                    if (deserialized.CheckErrorsExist()){
                        return Json(deserialized.GetErrors());
                    }
                    using (var conn = await Utils.GetAndOpenConnectionFactory()){
                        await deserialized.Save();    
                    }
                    if (deserialized.CheckIntegrityErrorsExist()){
                        return Json(deserialized.GetIntegriryErrors());
                    }
                    return Json(new {StudentId = deserialized.Id});
                }
                else {
                    return Json(400);
                }
            //}
            //catch (Exception) {
            //    return Json(400);
            //}
        }
    }
    [HttpPost]
    [Route("students/rus/new")]
    public async Task<JsonResult> AddRussianCitizenship(){
        using(var reader = new StreamReader(Request.Body)){
            var body = await reader.ReadToEndAsync();
            var settings = new JsonSerializerOptions();
            //try{
                var deserialized = JsonSerializer.Deserialize<RussianCitizenship>(body);
                if (deserialized!=null)
                {
                    if (deserialized.CheckErrorsExist()){
                        return Json(deserialized.GetErrors());
                    }
                    await deserialized.Save();    
                    if (deserialized.CheckIntegrityErrorsExist()){
                        return Json(deserialized.GetIntegriryErrors());
                    }
                    await StudentModel.LinkStudentAndCitizenship(typeof(RussianCitizenship), deserialized.StudentId, deserialized.Id);
                    return Json(new {RussianCitizenshipId = deserialized.Id});
                }
                else {
                    return Json(400);
                }
            //}
            //catch (Exception) {
            //    return Json(400);
            //}
        }
    }
}