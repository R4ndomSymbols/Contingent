using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using Contingent.Controllers.DTO.In;
using Contingent.Controllers.DTO.Out;
using Contingent.Models.Domain.Students;
using Contingent.Models.Domain.Specialities;
using Utilities;

namespace Contingent.Controllers;
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
        }
        var student = studentResult.ResultObject;
        try
        {
            var studentSaveResult = student.Save(savingTransaction);
            if (studentSaveResult.IsFailure)
            {
                return Json(new ErrorsDTO(studentSaveResult.Errors));
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