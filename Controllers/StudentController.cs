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
    [HttpGet]
    [Route("students/view/{query}")]
    public async Task<IActionResult> ViewStudent(string query)
    {   
        if (int.TryParse(query, out int id)){
            StudentModel? student = await StudentModel.GetStudentById(id);
            if (student == null){
                return View(@"Views/Shared/Error.cshtml", "Такого студента не существует");
            }
            return View(@"Views/Observe/Student.cshtml", student);
        }
        else{
            return View(@"Views/Shared/Error.cshtml", "Недопустимый id");
        }
        
    }
    // сначала идет адрес
    // затем студент,
    // затем прописка
    // затем российское гражданство 

    [HttpPost]
    [Route("students/addcomplex")]
    public async Task<IActionResult> AddComplex(){
        using var reader = new StreamReader(Request.Body);
        string jsonString = await reader.ReadToEndAsync();
        StudentComplexDTO? dto = null;
        try {
            dto = JsonSerializer.Deserialize<StudentComplexDTO>(jsonString);
        }
        catch (Exception){
            return BadRequest("Неверный формат данных");
        }
        if (dto is null){
            return BadRequest("Ошибка десериализации");
        }
        using NpgsqlConnection connection = await Utils.GetAndOpenConnectionFactory();
        using ObservableTransaction savingTransaction = new ObservableTransaction(await connection.BeginTransactionAsync(), connection);
        
        var actualAddress = AddressModel.Build(dto.ActualAddress);
        var factAddress = AddressModel.Build(dto.FactAddress);
        var rusCitizenship = RussianCitizenship.


        IEnumerable<ValidationError> cumulativeErrors = new List<ValidationError>();



            var actualAddress = JsonSerializer.Deserialize<AddressStringJSON>(actualAddressPart);
            var actualAddressObject = AddressModel.BuildFromString(actualAddress?.Address);
            if (actualAddressObject != null){
                if (actualAddressObject.CheckErrorsExist()){
                    cumulativeErrors = cumulativeErrors.Union(actualAddressObject.GetErrors());
                }
                else{
                    await actualAddressObject.Save(savingTransaction);
                }
            }
            var legalAddress = JsonSerializer.Deserialize<AddressStringJSON>(legalAddressPart);
            var legalAddressObject = AddressModel.BuildFromString(actualAddress?.Address);
            if (legalAddressObject !=  null){
                if (legalAddressObject.CheckErrorsExist()){
                    cumulativeErrors = cumulativeErrors.Union(legalAddressObject.GetErrors());
                }
                else{
                    await legalAddressObject.Save(savingTransaction);
                }
            }
            var rusCitizenship = JsonSerializer.Deserialize<RussianCitizenship>(russianCitizenshipPart);
            if (rusCitizenship is not null){
                if (rusCitizenship.CheckErrorsExist()){
                    cumulativeErrors = cumulativeErrors.Union(rusCitizenship.GetErrors());
                }
                else{
                    if (legalAddressObject!=null){
                        if (await legalAddressObject.GetCurrentState(savingTransaction) == RelationTypes.Bound){
                            await rusCitizenship.SetLegalAddressId(legalAddressObject.Id, savingTransaction);
                            await rusCitizenship.Save(savingTransaction);

                        }
                    }
                }
            }
            var student = JsonSerializer.Deserialize<StudentModel>(studentPart);
            if (student != null){
                if (student.CheckErrorsExist()){
                    cumulativeErrors = cumulativeErrors.Union(student.GetErrors());
                }
                else{
                    if (actualAddressObject != null && rusCitizenship!= null){
                        if (await actualAddressObject.GetCurrentState(savingTransaction) == RelationTypes.Bound
                        && await rusCitizenship.GetCurrentState(savingTransaction) == RelationTypes.Bound){
                            await student.SetActualAddress(actualAddressObject.Id, savingTransaction);
                            await student.SetRussianCitizenshipId(rusCitizenship.Id, savingTransaction);
                            await student.Save(savingTransaction);
                            
                        }
                    }
                }
            }
            
            if (cumulativeErrors.Any()){
                await savingTransaction.RollbackAsync();
                return Json(cumulativeErrors);
            }
            else{
                await savingTransaction.CommitAsync();
                return Json(new {
                    ActualAddressId = actualAddressObject?.Id ?? 0,
                    StudentId = student?.Id ?? 0,
                    LegalAddressId = legalAddressObject?.Id ?? 0,
                    RussianCitizenshipId = rusCitizenship?.Id ?? 0

                });
            }
        }
    }
    [HttpGet]
    [Route("students/tags")]
    public IActionResult GetTags(){
        var tags = EducationLevel.GetAllLevels().Select(x => new EducationalLevelRecordDTO(x));
        return Json(tags);
    }

}