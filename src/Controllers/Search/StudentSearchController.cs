using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using Contingent.Controllers.DTO.In;
using Contingent.Controllers.DTO.Out;
using Contingent.Models;
using Contingent.Models.Domain;
using Contingent.Models.Domain.Flow;
using Contingent.Models.Infrastruture;
using Contingent.SQL;
using Contingent.Statistics;
using Utilities;
namespace Contingent.Controllers.Search;



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
        try
        {
            dto = JsonSerializer.Deserialize<StudentSearchQueryDTO>(stream.ReadToEndAsync().Result);
        }
        catch (Exception e)
        {
            return BadRequest(e.Message);
        }
        if (dto is null)
        {
            return BadRequest("Неверный формат данных");
        }
        var search = new SearchHelper();
        var source = search.GetSource(dto.Source);
        var filter = search.GetFilter(dto);

        if (source != null)
        {
            var foundStudents = filter.Execute(source.Invoke());
            var foundSize = foundStudents.Count();
            return Json(foundStudents.Select(x => new StudentSearchResultDTO(x.Student, x.GroupTo)));
        }
        float count = 0;
        using (var conn = Utils.GetAndOpenConnectionFactory().Result)
        {
            var cmd = new NpgsqlCommand("SELECT reltuples AS estimate FROM pg_class WHERE relname = \'students\'", conn);
            using (cmd)
            {
                using var reader = cmd.ExecuteReader();
                reader.Read();
                count = (float)reader["estimate"];
            }
        }
        var result = new List<StudentFlowRecord>();
        var limits = new QueryLimits(dto.PageSkipCount, dto.PageSize, dto.PreciseOffset);
        var found = new List<StudentSearchResultDTO>();
        while (found.Count < limits.PageLength)
        {
            found.AddRange(filter.Execute(StudentHistory.GetLastRecordsForManyStudents(limits, (false, false)))
            .Select(x => new StudentSearchResultDTO(x.Student, x.GroupTo)));
            limits = new QueryLimits(dto.PageSize, dto.PageSize, limits.GlobalOffset + dto.PageSize);
            if (limits.GlobalOffset > count)
            {
                break;
            }
        }
        foreach (var searchResult in found)
        {
            searchResult.RequiredOffset = limits.GlobalOffset;
        }
        return Json(found);
    }
}