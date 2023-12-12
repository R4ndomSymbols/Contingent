using System.Diagnostics;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using StudentTracking.Models;
using StudentTracking.Models.Domain.Address;
using StudentTracking.Models.JSON;
using Utilities;
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
        await using var connection = await Utils.GetAndOpenConnectionFactory();
        var transaction = await connection.BeginTransactionAsync();
        await using ObservableTransaction saving = new ObservableTransaction(transaction, connection);
        await built.Save(saving);
        await saving.RollbackAsync();
        return Json(new { AboutAddress = await built.GetAddressInfo()});   
    }

    [HttpPost]
    [Route("/addresses/save/{address?}")]
    public async Task<JsonResult> CreateAddress(string? address)
    {

        var built = AddressModel.BuildFromString(address);
        if (built != null)
        {
             await using var connection = await Utils.GetAndOpenConnectionFactory();
            var transaction = await connection.BeginTransactionAsync();
            await using ObservableTransaction saving = new ObservableTransaction(transaction, connection);
            await built.Save(saving);
            if (await built.GetCurrentState(null) == RelationTypes.Bound)
            {
                return Json(new { AddressId = built.Id });
            }
        }
        return Json(new { AddressError = "Не удалось сохранить адрес" });
    }
}
