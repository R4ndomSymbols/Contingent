using System.Diagnostics;
using System.Runtime.Serialization.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;
using StudentTracking.Models.Domain;

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
    public IActionResult ProcessStudent(string query)
    {   
        if (query == "new"){
            return View(@"Views/Models/StudentModify.cshtml", new StudentModel());
        }
        if (int.TryParse(query, out int id)){
            StudentModel? student = StudentModel.GetStudentById(id);
            if (student == null){
                return View(@"Views/Shared/Error.cshtml", "Такого студента не существует");
            }
            return View(@"Views/Models/StudentModify.cshtml", student);
        }
        else{
            return View(@"Views/Shared/Error.cshtml", "Недопустимый id");
        }
        
    }
    [HttpPost]
    [Route("/students/add")]
    public async Task<IActionResult> CreateStudent(){
        using(var reader = new StreamReader(Request.Body)){
            var body = await reader.ReadToEndAsync();
            var settings = new JsonSerializerOptions();
            var deserialized = JsonSerializer.Deserialize<StudentModel>(body);
            if (deserialized!=null){
                StudentModel.SaveStudent(deserialized);
            }
        }
        return Ok();
    }
    [HttpGet]
    [Route("/student/genders")]
    public JsonResult GetGenders(){
        var toSend = StudentModel.GetGenders();
        return Json(toSend.ToArray());
    }
}