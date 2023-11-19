using System.Diagnostics;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using StudentTracking.Models;
using StudentTracking.Models.Domain.Address;
using StudentTracking.Models.JSON;

namespace StudentTracking.Controllers;

public class AddressController : Controller
{
    private readonly ILogger<AddressController> _logger;

    public AddressController(ILogger<AddressController> logger)
    {
        _logger = logger;
    }
    [HttpGet]
    [Route("/addresses/federal")]
    public JsonResult GetAllCountryRegions(){
        var dbResponce = FederalSubject.GetAll().Select(x => new FederalSubjectJSON() 
        {
            Code = int.Parse(x.Code),
            FullName = x.LongTypedName    
        });
        if (dbResponce!=null){
            return Json(dbResponce.ToArray());
        }
        return Json(null);   
    }
    [HttpGet]
    [Route ("/addresses/suggest/{suggest?}")]
    public JsonResult GetSuggestions(string? suggest){
        if (suggest == null){
            return Json(new object());
        }
        else {
            return Json(AddressModel.GetNextSuggestions(suggest));
        }
    }

    [HttpPost]
    [Route("/addresses/new/{address?}")]
    public JsonResult CreateAddress(string? address){
      
            var built = AddressModel.BuildFromString(address);
            if (built!=null){
                if (built.CheckErrorsExist()){
                    return Json(built.GetErrors());
                }
                else{
                    return Json(built.Id);
                }
            }
            else{
                return Json(new object());
            }
        }   
}
