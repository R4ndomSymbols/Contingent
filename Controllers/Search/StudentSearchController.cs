using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using StudentTracking.Controllers.DTO.In;
using StudentTracking.Controllers.DTO.Out;
using StudentTracking.Models;
using StudentTracking.Models.Domain.Flow;
using StudentTracking.Models.Infrastruture;
using StudentTracking.SQL;
using StudentTracking.Statistics;
namespace StudentTracking.Controllers.Search;



public class StudentSearchController : Controller
{

    private readonly ILogger<StudentSearchController> _logger;

    public StudentSearchController(ILogger<StudentSearchController> logger)
    {
        _logger = logger;
    }
    // головное меню поиска
    [HttpGet]
    [Route("/students/search/menu")]
    public IActionResult FindStudentsMainPage()
    {
        return View(@"Views/Search/Students.cshtml", new object());

    }
    // поиск студента по паратетрам из тела запроса
    [HttpPost]
    [Route("students/search/query")]
    public IActionResult FindStudents()
    {
        var stream = new StreamReader(Request.Body);
        StudentSearchQueryDTO? dto = null;
        try {
            dto = JsonSerializer.Deserialize<StudentSearchQueryDTO>(stream.ReadToEndAsync().Result);
        }
        catch (Exception e){
            return BadRequest(e.Message);
        }
        if (dto is null){
            return BadRequest("Неверный формат данных");
        }
        var search = new SearchHelper();
        var source = search.GetSource(dto.Source);
        var filter = search.GetFilter(dto);

        if (source!=null){
            var found = filter.Execute(source.Invoke());
            var foundSize = found.Count(); 
            return Json(found.Select(x => new StudentSearchResultDTO(x.Student, x.GroupTo, foundSize)));   
        }

        var result = new List<StudentFlowRecord>();
        int pageOffset = 0;
        int maxOffset = 0;
        while (result.Count < dto.PageSize)
        {   
            maxOffset = dto.GlobalOffset + pageOffset;
            var limits = new QueryLimits(0, dto.PageSize, maxOffset);
            var found = StudentHistory.GetLastRecordsForManyStudents(limits, (false, false));
            if (!found.Any())
            {
                break;
            }
            result.AddRange(filter.Execute(found));
            pageOffset+=dto.PageSize;
        }
        return Json(result.Take(dto.PageSize).Select(x => new StudentSearchResultDTO(x.Student, x.GroupTo, maxOffset)));
    }      
}