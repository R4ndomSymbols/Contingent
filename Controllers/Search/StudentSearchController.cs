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
    [HttpGet]
    [Route("/students/search/menu")]
    public IActionResult FindStudentsMainPage()
    {
        return View(@"Views/Search/Students.cshtml", new object());

    }
    [HttpPost]
    [Route("students/search/query")]
    public async Task<JsonResult> FindStudents()
    {
        using var stream = new StreamReader(Request.Body);
        StudentSearchQueryDTO? query = null;
        try
        {
            var text = await stream.ReadToEndAsync();
            query = JsonSerializer.Deserialize<StudentSearchQueryDTO>(text);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return Json(new ErrorsDTO(new ValidationError(nameof(FindStudents), e.Message)));
        }
        if (query is null)
        {
            return Json(new ErrorsDTO(new ValidationError(nameof(FindStudents), "Запрос не может быть пустым")));
        }
        var search = new SearchHelper();
        var source = search.GetSource(query.Source);
        var filter = search.GetFilter(query);

        if (source!=null){
            var found = filter.Execute(source.Invoke());
            var foundSize = found.Count(); 
            return Json(found.Select(x => new StudentSearchResultDTO(x.Student, x.GroupTo, foundSize)));   
        }

        var result = new List<StudentFlowRecord>();
        int pageOffset = 0;
        int maxOffset = 0;
        while (result.Count < query.PageSize)
        {   
            maxOffset = query.GlobalOffset + pageOffset;
            var limits = new QueryLimits(0, query.PageSize, maxOffset);
            var found = StudentHistory.GetLastRecordsForManyStudents(limits, (false, false));
            if (!found.Any())
            {
                break;
            }
            result.AddRange(filter.Execute(found));
            pageOffset+=query.PageSize;
        }
        return Json(result.Take(query.PageSize).Select(x => new StudentSearchResultDTO(x.Student, x.GroupTo, maxOffset)));
    }      
}