using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Contingent.Controllers.DTO.In;
using Contingent.Controllers.DTO.Out;
using Contingent.Models.Domain.Specialties;

namespace Contingent.Controllers;


public class SpecialtyController : Controller
{

    private readonly ILogger<SpecialtyController> _logger;

    public SpecialtyController(ILogger<SpecialtyController> logger)
    {
        _logger = logger;
    }

    [HttpGet]
    [Route("specialities/modify/{query}")]
    public IActionResult ProcessSpecialty(string query)
    {
        if (query == "new")
        {
            return View(@"Views/Modify/SpecialityModify.cshtml", new SpecialityOutDTO());
        }
        else if (int.TryParse(query, out int id))
        {
            var got = SpecialtyModel.GetById(id, null).Result;
            if (got != null)
            {
                return View(@"Views/Modify/SpecialityModify.cshtml", new SpecialityOutDTO(got));
            }
            else
            {
                return View(@"Views/Shared/Error.cshtml", "Специальности с таким id не существует");
            }

        }
        else
        {
            return View(@"Views/Shared/Error.cshtml", "Недопустимый id специальности");
        }
    }
    [HttpPost]
    [Route("specialities/add")]
    public async Task<IActionResult> AddOrUpdateSpecialty()
    {
        using var reader = new StreamReader(Request.Body);
        var body = await reader.ReadToEndAsync();
        SpecialtyInDTO? dto;
        try
        {
            dto = JsonSerializer.Deserialize<SpecialtyInDTO>(body);
        }
        catch (Exception)
        {
            return BadRequest(ErrorCollectionDTO.GetGeneralError("Неверный формат данных"));
        }

        if (dto != null)
        {
            var result = SpecialtyModel.Build(dto);
            if (result.IsFailure)
            {
                return Json(new ErrorCollectionDTO(result.Errors));
            }
            else
            {
                result.ResultObject.Save();
                return Json(new SpecialityOutDTO(result.ResultObject));
            }
        }
        return BadRequest(ErrorCollectionDTO.GetGeneralError("Неверный формат данных"));
    }

    [HttpGet]
    [Route("specialities/about/{query?}")]
    public IActionResult ViewSpeciality(string? query)
    {
        if (int.TryParse(query, out int id))
        {
            var got = SpecialtyModel.GetById(id, null).Result;
            if (got != null)
            {
                return View(@"Views/Observe/Speciality.cshtml", new SpecialityOutDTO(got));
            }
            else
            {
                return View(@"Views/Shared/Error.cshtml", "Специальности с таким id не существует");
            }

        }
        else
        {
            return View(@"Views/Shared/Error.cshtml", "Недопустимый id специальности");
        }
    }
}