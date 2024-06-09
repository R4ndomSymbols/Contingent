using Microsoft.AspNetCore.Mvc;
using Contingent.Controllers.DTO.In;
using Contingent.Controllers.DTO.Out;
using Contingent.Models.Domain.Address;
using Contingent.Utilities;

namespace Contingent.Controllers;

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
    public IActionResult GetAddressInfo(string? address)
    {
        Result<AddressModel> result = AddressModel.Create(new AddressInDTO() { Address = address });
        if (result.IsFailure)
        {
            return BadRequest(new ErrorCollectionDTO(result.Errors));
        }
        return Json(new { AddressState = result.ResultObject?.GetAddressInfo() ?? "" });
    }

    [HttpPost]
    [Route("/addresses/save/{address?}")]
    public async Task<IActionResult> CreateAddress(string? address)
    {

        var result = AddressModel.Create(new AddressInDTO() { Address = address });
        if (result.IsFailure)
        {
            return BadRequest(new ErrorCollectionDTO(result.Errors));
        }
        var built = result.ResultObject!;
        using var connection = await Utils.GetAndOpenConnectionFactory();
        using var transaction = await connection.BeginTransactionAsync();
        using ObservableTransaction savingTransaction = new ObservableTransaction(transaction, connection);
        var savingResult = await built.Save(savingTransaction);
        if (savingResult.IsFailure)
        {
            return BadRequest(new ErrorCollectionDTO(savingResult.Errors));
        }
        return Json(new { AddressId = built });
    }
}
