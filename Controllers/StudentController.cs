using System.Diagnostics;
using System.Runtime.Serialization.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;
using Npgsql;
using StudentTracking.Controllers.DTO.In;
using StudentTracking.Controllers.DTO.Out;
using StudentTracking.Models.Domain;
using StudentTracking.Models.Domain.Address;
using StudentTracking.Models.Domain.Misc;
using StudentTracking.Models.JSON;
using Utilities;
using Utilities.Validation;

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
        StudentComplexDTO? dto = null;
        try
        {
            dto = JsonSerializer.Deserialize<StudentComplexDTO>(jsonString);
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
        var rusCitizenshipResult = RussianCitizenship.Build(dto.RusCitizenship);
        var studentResult = StudentModel.Build(dto.Student);
        var studentTagsResults = dto.Education.Select(x => StudentEducationalLevelRecord.Create(x));

        
        if (ResultHelper.AnyFailure(rusCitizenshipResult, studentResult) || studentTagsResults.Any(x => x.IsFailure))
        {   
            var errors = ResultHelper.MergeErrors(rusCitizenshipResult, studentResult).ToList();
            if (studentTagsResults.Any(x => x.IsFailure)){
                errors.AddRange(studentTagsResults.Where(x => x.IsFailure).First().Errors);
            }
            var err = new ErrorsDTO(errors);
            return Json(err);
        }
        
        var rusCitizenship = rusCitizenshipResult.ResultObject;
        var student = studentResult.ResultObject;
        var tags = studentTagsResults.Select(x => x.ResultObject);

        try
        {
            // инициализация адреса в таком порядке, чтобы не создавались копии
             // реальный адрес проживания
            var actualAddressResult = AddressModel.Create(dto.ActualAddress, savingTransaction);
            if (actualAddressResult.IsFailure){
                return Json(new ErrorsDTO(actualAddressResult.Errors));
            }
            var actualAddress = actualAddressResult.ResultObject;
            await actualAddress.Save(savingTransaction);
            // прописка
            var factAddressResult = AddressModel.Create(dto.FactAddress, savingTransaction);
            if (factAddressResult.IsFailure){
                return Json(new ErrorsDTO(factAddressResult.Errors));
            }
            var factAddress = factAddressResult.ResultObject;
            await factAddress.Save(savingTransaction);

            // проверка адресации не проходит, адрес каждый раз новый
            // дописать потом
            var result = await actualAddress.Save(savingTransaction);
            if (result.IsFailure){
                return Json(new ErrorsDTO(result.Errors));
            }
            result = await factAddress.Save(savingTransaction);
            if (result.IsFailure){
                return Json(new ErrorsDTO(result.Errors));
            }

            rusCitizenship.LegalAddressId = factAddress.Id;
            if (await RussianCitizenship.IsIdExists(rusCitizenship.Id, savingTransaction)){
                await rusCitizenship.Update(savingTransaction);
            }
            else {
                await rusCitizenship.Save(savingTransaction);
            }
            student.ActualAddressId = actualAddress.Id;
            student.RussianCitizenshipId = rusCitizenship.Id;

            if (await StudentModel.IsIdExists(student.Id, savingTransaction)){
                await student.Update(savingTransaction);
            }
            else {
                await student.Save(savingTransaction);
            }
            var records = await StudentEducationalLevelRecord.GetByOwner(student.Id);
            // пропуск имеющийся тегов
            if (records.Any()){
                tags = tags.Where(x => !records.Any(y => y.Level == x.Level));
            }
            foreach (var tag in tags){
                tag.OwnerId = student.Id;
                await tag.SaveRecord(savingTransaction);
            }
        }
        catch (Exception e)
        {
            await savingTransaction.RollbackAsync();
            return Json(new ErrorsDTO(new ValidationError("general", e.Message)));
        }
        await savingTransaction.CommitAsync();
        return Json(new
        {
            ActualAddressId = student.ActualAddressId,
            StudentId = student.Id,
            LegalAddressId = rusCitizenship.LegalAddressId,
            RussianCitizenshipId = rusCitizenship.Id

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