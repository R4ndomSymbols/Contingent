using System.Diagnostics;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using StudentTracking.Models;
using StudentTracking.Models.Domain.Address;
using StudentTracking.Models.JSON;
using Utilities.Validation;

namespace StudentTracking.Controllers;

public class AddressController : Controller
{
    private readonly ILogger<AddressController> _logger;

    public AddressController(ILogger<AddressController> logger)
    {
        _logger = logger;
    }
    [HttpGet]
    [Route("/addresses/suggest/{suggest?}")]
    public async Task<JsonResult> GetSuggestions(string? suggest)
    {
        if (suggest == null)
        {
            return Json(new object());
        }
        else
        {
            return Json(await AddressModel.GetNextSuggestions(suggest));
        }
    }
    [HttpGet]
    [Route("/addresses/explain/{address?}")]
    public async Task<JsonResult> GetAddressInfo(string? address)
    {
        var built = AddressModel.BuildFromString(address?.ToLower());
        if (built == null)
        {
            return Json(new { AboutAddress = "Введен пустой адрес"});
        }
        var errors = built.GetErrors();
        if (errors.Any())
        {
            return Json(new { AboutAddress = errors.First().ToUserString() });
        }
        string status = "";
        Action updateStatus = async () => status = await built.GetAddressInfo();
        await built.Save(false, updateStatus, updateStatus);
        return Json(new { AboutAddress = status });   
    }

    [HttpPost]
    [Route("/addresses/save/{address?}")]
    public async Task<JsonResult> CreateAddress(string? address)
    {

        var built = AddressModel.BuildFromString(address);
        if (built != null)
        {
            await built.Save(true, null, null);
            if (await built.GetCurrentState(null) == RelationTypes.Bound)
            {
                return Json(new { AddressId = built.Id });
            }
        }
        return Json(new { AddressError = "Не удалось сохранить адрес" });
    }
}
