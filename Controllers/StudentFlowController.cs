
using StudentTracking.Models;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json.Serialization;
using System.Text.Json;
namespace StudentTracking.Controllers;
public class StudentFlowController : Controller{

    private readonly ILogger<StudentFlowController> _logger;

    public StudentFlowController(ILogger<StudentFlowController> logger)
    {
        _logger = logger;
    }
    /*

    [HttpGet]
    [Route("/flow")]
    public ActionResult Index(){
        return View("~/Views/Processing/StudentFlow.cshtml");
    }

    [HttpPost]
    [Route("/flow/filter")]
    public async Task<JsonResult> FilterStudentsByCriteria(){
        using (var reader = new StreamReader(Request.Body)){
            var data = await reader.ReadToEndAsync();
            var deserialized = JsonSerializer.Deserialize<FilterRequest>(data);
            if (deserialized == null){
                return Json(null);
            }
            try {
                var result = StudentRecord.FilterByNextOrderType((OrderTypesId)deserialized.OrderTypeId, deserialized.OrderId, deserialized.SearchName, deserialized.SearchGroupName);
                if (result == null){
                    return Json(null);
                }
                else{
                    return Json(result.ToArray());
                }
            }
            catch (InvalidCastException){
                return Json(null);
            }
        }
    }
    [HttpGet]
    [Route("/flow/pinned/{query}")]
    public JsonResult GetStudentsPinnedToOrder(string query){
        if (int.TryParse(query, out int id)){
            var found = StudentRecord.GetAssociatedStudents(id);
            if (found == null){
                return Json(null);
            }
            else{
                return Json(found.ToArray());
            }
        }
        else{
            return Json(null);
        }
    }
    [HttpPost]
    [Route("/flow/save")]
    public async Task<JsonResult> SaveRecords(){
        using (var reader = new StreamReader(Request.Body)){
            var data = await reader.ReadToEndAsync();
            var result = JsonSerializer.Deserialize<List<StudentRecord>>(data);
            if (result == null){
                return Json(null);
            }
            StudentRecord.SaveRecords(result);
            return Json(null);
        }
    }
}
[Serializable]
public class FilterRequest{
    [JsonPropertyName("orderTypeId")]
    public int OrderTypeId {get; set;}
    [JsonPropertyName("orderId")]
    public int OrderId { get; set; }
    [JsonPropertyName("searchName")]
    public string SearchName {get; set; }
    [JsonPropertyName("searchGroupName")]
    public string SearchGroupName {get; set;}

    public FilterRequest (){
        OrderTypeId = -1;
        OrderId = -1;
        SearchName = "";
        SearchGroupName = "";
    }

*/
}