using Microsoft.AspNetCore.Mvc;
using StudentTracking.Controllers.DTO.In;
using StudentTracking.Controllers.DTO.Out;
using StudentTracking.Models.Domain.Address;
using Utilities;

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
    public JsonResult GetSuggestions(string? suggest)
    {
        return Json(AddressModel.GetNextSuggestions(suggest));
    }
    [HttpGet]
    [Route("/addresses/explain/{address?}")]
    public JsonResult GetAddressInfo(string? address)
    {
        Result<AddressModel?> result = AddressModel.Create(new AddressDTO(){ Address = address});
        if (result.IsFailure)
        {
            return Json(new ErrorsDTO(result.Errors));
        }
        return Json(new { AddressState = result.ResultObject?.GetAddressInfo() ?? ""});   
    }

    [HttpPost]
    [Route("/addresses/save/{address?}")]
    public async Task<JsonResult> CreateAddress(string? address)
    {

        var result = AddressModel.Create(new AddressDTO(){ Address = address});
        if (result.IsFailure){
            return Json(new ErrorsDTO(result.Errors));
        }
        if (result.ResultObject is null){
            throw new Exception("Suppression");
        }
        var built = result.ResultObject;
        using var connection = await Utils.GetAndOpenConnectionFactory();
        using var transaction = await connection.BeginTransactionAsync();
        using ObservableTransaction savingTransaction = new ObservableTransaction(transaction, connection);
        var savingResult = await built.Save(savingTransaction);
        if (savingResult.IsFailure){
            return Json(new ErrorsDTO(savingResult.Errors));
        }
        return Json(new { AddressId = built});
    }
}
