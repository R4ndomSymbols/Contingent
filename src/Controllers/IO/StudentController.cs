using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using Contingent.Controllers.DTO.In;
using Contingent.Controllers.DTO.Out;
using Contingent.Models.Domain.Students;
using Contingent.Models.Domain.Specialties;
using Contingent.Utilities;
using Microsoft.AspNetCore.Authorization;
using Contingent.DTOs.Out;
using Contingent.Models.Domain.Flow;
using Contingent.Models.Domain.Orders.OrderData;

namespace Contingent.Controllers;
public class StudentController : Controller
{
    private readonly ILogger<HomeController> _logger;

    public StudentController(ILogger<HomeController> logger)
    {
        _logger = logger;
    }

    [HttpGet]
    [AllowAnonymous]
    [Route("/students/modify/{query}")]
    public IActionResult GetStudentPageModify(string query)
    {
        return View(@"Views/Auth/JWTHandler.cshtml", new RedirectOptions()
        {
            DisplayURL = "/protected/students/modify/" + query,
            RequestType = "GET",
        });
    }


    // отрисовка страницы редактирования и добавления
    [HttpGet]
    [Authorize(Roles = "Admin")]
    [Route("/protected/students/modify/{query}")]
    public IActionResult GetStudentPageModifyProtected(string query)
    {
        if (query == "new")
        {
            return View(@"Views/Modify/StudentModify.cshtml", new StudentFullDTO());
        }
        if (int.TryParse(query, out int id))
        {
            StudentModel? student = StudentModel.GetStudentById(id);
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
    [AllowAnonymous]
    [Route("/students/view/{id:int}")]
    public IActionResult GetStudentPageView(int id)
    {
        return View(@"Views/Auth/JWTHandler.cshtml", new RedirectOptions()
        {
            DisplayURL = "/protected/students/view/" + id,
            RequestType = "GET",
        });
    }


    [HttpGet]
    [Authorize(Roles = "Admin")]
    [Route("/protected/students/view/{id:int}")]
    public IActionResult ViewStudent(int id)
    {
        StudentModel? student = StudentModel.GetStudentById(id);
        if (student == null)
        {
            return View(@"Views/Shared/Error.cshtml", "Такого студента не существует");
        }
        return View(@"Views/Observe/Student.cshtml", new StudentFullDTO(student));

    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [Route("/students/addcomplex")]
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
            return BadRequest(ErrorCollectionDTO.GetGeneralError("Неверный формат данных, " + e.Message));
        }
        if (dto is null)
        {
            return BadRequest(ErrorCollectionDTO.GetGeneralError("Ошибка десериализации"));
        }
        using NpgsqlConnection connection = await Utils.GetAndOpenConnectionFactory();
        using ObservableTransaction savingTransaction = new ObservableTransaction(await connection.BeginTransactionAsync(), connection);
        var studentResult = StudentModel.Build(dto, savingTransaction);

        if (studentResult.IsFailure)
        {
            return BadRequest(studentResult.Errors.AsErrorCollection());
        }
        var student = studentResult.ResultObject;
        try
        {
            var studentSaveResult = student.Save(savingTransaction);
            if (studentSaveResult.IsFailure)
            {
                return BadRequest(studentSaveResult.Errors.AsErrorCollection());
            }
        }
        catch (Exception e)
        {
            await savingTransaction.RollbackAsync();
            return BadRequest(ErrorCollectionDTO.GetCriticalError("Не удалось сохранить студента: " + e.Message));
        }
        await savingTransaction.CommitAsync();
        return Json(new
        {
            ActualAddressId = student.ActualAddressId,
            StudentId = student.Id,
            LegalAddressId = student.RussianCitizenship!.LegalAddressId,
            RussianCitizenshipId = student.RussianCitizenship.Id

        });

    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    [Route("/students/tags")]
    public IActionResult GetTags()
    {
        var tags = LevelOfEducation.ListOfLevels.Select(x => new EducationalLevelRecordDTO(x));
        return Json(tags);
    }

    [HttpGet]
    [AllowAnonymous]
    [Route("/students/vacations")]
    public IActionResult ViewVacations()
    {
        return View(@"Views/Auth/JWTHandler.cshtml", new RedirectOptions()
        {
            DisplayURL = "/protected/students/vacations",
            RequestType = "GET",
        });

    }


    [HttpGet]
    [Authorize(Roles = "Admin")]
    [Route("/protected/students/vacations")]
    public IActionResult GetCurrentVacations()
    {
        var current = StudentHistory.GetStudentByOrderState(
            DateTime.Now.AddYears(5),
            OrderTypeInfo.AcademicVacationSendTypes,
            OrderTypeInfo.AcademicVacationCloseTypes,
            null
        );
        var dtos = current.Select(x =>
        {
            var last = x.GetHistory(null).GetLastRecord() ?? throw new Exception("По каким-то причинам нет записей");
            return new StudentHistoryMoveDTO(
                last.StudentNullRestrict,
                null,
                null,
                last.OrderNullRestrict,
                last.StatePeriod
            );
        });
        return View(@"Views/Observe/Vacations.cshtml", dtos);
    }

}