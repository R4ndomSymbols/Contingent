using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Contingent.Controllers.DTO.In;
using Contingent.Controllers.DTO.Out;
using Contingent.Models.Domain.Specialities;
using Contingent.Models.Infrastruture;
using Contingent.SQL;

namespace Contingent.Controllers.Search;


public class SpecialitySearchController : Controller
{

    public SpecialitySearchController()
    {

    }

    [HttpGet]
    [Route("specialities/search/menu")]
    public IActionResult GetMainPage()
    {
        return View("Views/Search/Specialities.cshtml");
    }

    [HttpPost]
    [Route("specialities/search/query")]
    public async Task<JsonResult> SearchSpecialities()
    {
        using var reader = new StreamReader(Request.Body);
        SpecialitySearchQueryDTO? dto = null;
        try
        {
            dto = JsonSerializer.Deserialize<SpecialitySearchQueryDTO>(await reader.ReadToEndAsync());
        }
        catch (Exception e)
        {
            return Json(
                new ErrorsDTO(
                    new ValidationError(e.Message)
                )
            );
        }
        if (dto is null)
        {
            return Json(new ErrorsDTO(new ValidationError("Десериализация поискового запроса провалилась")));
        }
        var found = await SpecialityModel.FindSpecialities(new QueryLimits(0, 100));
        var filter = new SearchHelper().GetFilterForSpecialities(dto);
        return Json(filter.Execute(found).Select(x => new SpecialitySearchResultDTO(x)));
    }

    [HttpGet]
    [Route("specialities/suggest/{query?}")]
    public async Task<JsonResult> GetSuggestions(string? query)
    {
        return Json(await SpecialityModel.GetSuggestions(query, null));
    }

}