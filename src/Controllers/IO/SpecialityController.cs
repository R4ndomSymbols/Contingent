using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Contingent.Controllers.DTO.In;
using Contingent.Controllers.DTO.Out;
using Contingent.Models.Domain.Specialties;
using Microsoft.AspNetCore.Authorization;
using Contingent.DTOs.Out;

namespace Contingent.Controllers;


public class SpecialtyController : Controller
{

    private readonly ILogger<SpecialtyController> _logger;

    public SpecialtyController(ILogger<SpecialtyController> logger)
    {
        _logger = logger;
    }
    [HttpGet]
    [AllowAnonymous]
    [Route("/specialties/modify/{query}")]
    public IActionResult GetProcessingPage(string query)
    {
        return View(@"Views/Auth/JWTHandler.cshtml", new RedirectOptions()
        {
            DisplayURL = "/protected/specialties/modify/" + query,
            RequestType = "GET",
        });
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    [Route("/protected/specialties/modify/{query}")]
    public IActionResult ProcessSpecialty(string query)
    {
        if (query == "new")
        {
            return View(@"Views/Modify/SpecialtyModify.cshtml", new SpecialityOutDTO());
        }
        else if (int.TryParse(query, out int id))
        {
            var got = SpecialtyModel.GetById(id, null).Result;
            if (got != null)
            {
                return View(@"Views/Modify/SpecialtyModify.cshtml", new SpecialityOutDTO(got));
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
    [Authorize(Roles = "Admin")]
    [Route("/specialties/add")]
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
                return BadRequest(new ErrorCollectionDTO(result.Errors));
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
    [AllowAnonymous]
    [Route("/specialties/view/{query}")]
    public IActionResult GetViewPage(string query)
    {
        return View(@"Views/Auth/JWTHandler.cshtml", new RedirectOptions()
        {
            DisplayURL = "/protected/specialties/view/" + query,
            RequestType = "GET",
        });
    }


    [HttpGet]
    [Authorize(Roles = "Admin")]
    [Route("/protected/specialties/view/{query?}")]
    public IActionResult ViewSpecialty(string? query)
    {
        if (int.TryParse(query, out int id))
        {
            var got = SpecialtyModel.GetById(id, null).Result;
            if (got != null)
            {
                return View(@"Views/Observe/Specialty.cshtml", new SpecialityOutDTO(got));
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