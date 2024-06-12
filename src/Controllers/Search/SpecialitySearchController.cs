using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Contingent.Controllers.DTO.In;
using Contingent.Controllers.DTO.Out;
using Contingent.Models.Domain.Specialties;
using Contingent.Models.Infrastructure;
using Contingent.SQL;
using Microsoft.AspNetCore.Authorization;
using Contingent.DTOs.Out;

namespace Contingent.Controllers.Search;


public class SpecialtySearchController : Controller
{

    public SpecialtySearchController()
    {

    }

    [HttpGet]
    [AllowAnonymous]
    [Route("/specialties/search")]
    public IActionResult GetMainPage()
    {
        return View(@"Views/Auth/JWTHandler.cshtml", new RedirectOptions()
        {
            DisplayURL = "/protected/specialties/search",
            RequestType = "GET",
        });
    }


    [HttpGet]
    [Authorize(Roles = "Admin")]
    [Route("/protected/specialties/search")]
    public IActionResult GetMainPageProtected()
    {
        return View("Views/Search/Specialties.cshtml");
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [Route("/specialties/search/find")]
    public async Task<IActionResult> SearchSpecialties()
    {
        using var reader = new StreamReader(Request.Body);
        SpecialtySearchQueryDTO? dto = null;
        try
        {
            dto = JsonSerializer.Deserialize<SpecialtySearchQueryDTO>(await reader.ReadToEndAsync());
        }
        catch (Exception e)
        {
            return BadRequest(ErrorCollectionDTO.GetGeneralError("Неверный поисковый запрос: " + e.Message));
        }
        if (dto is null)
        {
            return BadRequest(ErrorCollectionDTO.GetGeneralError("Неверный поисковый запрос"));
        }
        var found = await SpecialtyModel.FindSpecialties(new QueryLimits(0, 100));
        var filter = new SearchHelper().GetFilterForSpecialties(dto);
        return Json(filter.Execute(found).Select(x => new SpecialtySearchResultDTO(x)));
    }
}