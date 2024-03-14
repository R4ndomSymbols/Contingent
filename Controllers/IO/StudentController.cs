using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using StudentTracking.Controllers.DTO.In;
using StudentTracking.Controllers.DTO.Out;
using StudentTracking.Models.Domain;
using StudentTracking.Models.Domain.Misc;
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
        if (query == "new")
        {
            return View(@"Views/Modify/StudentModify.cshtml", new StudentFullDTO());
        }
        if (int.TryParse(query, out int id))
        {
            StudentModel? student = await StudentModel.GetStudentById(id);
            if (student == null)
            {
                return View(@"Views/Shared/Error.cshtml", "Такого студента не существует");
            }
            return View(@"Views/Modify/StudentModify.cshtml", new StudentFullDTO(student));
        }
        else
        {
            return View(@"Views/Shared/Error.cshtml", "Недопустимый id");
        }

    }
    [HttpGet]
    [Route("students/view/{id?}")]
    public async Task<IActionResult> ViewStudent(string id)
    {
        if (int.TryParse(id, out int parsed))
        {
            StudentModel? student = await StudentModel.GetStudentById(parsed);
            if (student == null)
            {
                return View(@"Views/Shared/Error.cshtml", "Такого студента не существует");
            }
            return View(@"Views/Observe/Student.cshtml", new StudentFullDTO(student));
        }
        else
        {
            return View(@"Views/Shared/Error.cshtml", "Недопустимый id");
        }
    }
    // сначала идет адрес
    // затем студент,
    // затем прописка
    // затем российское гражданство 

    [HttpPost]
    [Route("students/addcomplex")]
    public async Task<IActionResult> AddComplex()
    {
        using var reader = new StreamReader(Request.Body);
        string jsonString = await reader.ReadToEndAsync();
        StudentInDTO? dto = null;
        try
        {
            dto = JsonSerializer.Deserialize<StudentInDTO>(jsonString);
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            return BadRequest("Неверный формат данных");
        }
        if (dto is null)
        {
            return BadRequest("Ошибка десериализации");
        }

        using NpgsqlConnection connection = await Utils.GetAndOpenConnectionFactory();
        using ObservableTransaction savingTransaction = new ObservableTransaction(await connection.BeginTransactionAsync(), connection);
        var studentResult = StudentModel.Build(dto);
        
        if (studentResult.IsFailure)
        {   
            return Json(new ErrorsDTO(studentResult.Errors));
            var errors = studentResult.Errors.ToList();
         
        }
        
        var student = studentResult.ResultObject;
        

        try
        {
            var studentSaveResult = await student.Save(savingTransaction);
            if (studentSaveResult.IsFailure){
                return Json(new ErrorsDTO(studentSaveResult.Errors)); 
            }
            // внутренняя зависимость, студент должен быть сохранен прежде запроса тегов
            var studentTagsResults = dto.Education.Select(x => StudentEducationalLevelRecord.Create(x, student));
            if (studentTagsResults.Any(x => x.IsFailure)){
                return Json(new ErrorsDTO(studentTagsResults.Where(x => x.IsFailure).First().Errors));
            }
            var tags = studentTagsResults.Select(x => x.ResultObject);
            var records = await StudentEducationalLevelRecord.GetByOwner(student);
            Console.WriteLine(student.Id.Value);
            // пропуск имеющийся тегов
            if (records.Any()){
                tags = tags.Where(x => !records.Any(y => y.Level == x.Level));
            }
            foreach (var tag in tags){
                await tag.SaveRecord(savingTransaction);
            }
        }
        catch (Exception e)
        {
            await savingTransaction.RollbackAsync();
            return Json(new ErrorsDTO(new ValidationError("general", e.Message + " " + e.StackTrace)));
        }
        await savingTransaction.CommitAsync();
        return Json(new
        {
            ActualAddressId = student.ActualAddressId,
            StudentId = student.Id,
            LegalAddressId = student.RussianCitizenship.LegalAddressId,
            RussianCitizenshipId = student.RussianCitizenship.Id

        });

    }

    [HttpGet]
    [Route("students/tags")]
    public IActionResult GetTags()
    {
        var tags = LevelOfEducation.ListOfLevels.Select(x => new EducationalLevelRecordDTO(x));
        return Json(tags);
    }

}