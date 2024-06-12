using Microsoft.AspNetCore.Mvc;
using Contingent.Controllers.DTO.In;
using Contingent.Controllers.DTO.Out;
using Contingent.Models.Domain.Address;
using Contingent.Utilities;
using Microsoft.AspNetCore.Authorization;

namespace Contingent.Controllers;

public class AddressController : Controller
{
    private readonly ILogger<AddressController> _logger;

    public AddressController(ILogger<AddressController> logger)
    {
        _logger = logger;
    }
    [HttpGet]
    [Authorize(Roles = "Admin")]
    [Route("/addresses/suggest/{suggest?}")]
    public JsonResult GetSuggestions(string? suggest)
    {
        return Json(AddressModel.GetNextSuggestions(suggest, null));
    }
    [HttpGet]
    [Authorize(Roles = "Admin")]
    [Route("/addresses/explain/{address?}")]
    public IActionResult GetAddressInfo(string? address)
    {
        Result<AddressModel> result = AddressModel.Create(new AddressInDTO() { Address = address }, null);
        if (result.IsFailure)
        {
            return BadRequest(new ErrorCollectionDTO(result.Errors));
        }
        return Json(new { AddressState = result.ResultObject?.GetAddressInfo() ?? "" });
    }
}
