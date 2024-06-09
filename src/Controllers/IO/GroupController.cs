using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Contingent.Controllers.DTO.In;
using Contingent.Controllers.DTO.Out;
using Contingent.Models.Domain.Groups;
using Contingent.Models.Domain.Students;
using Contingent.Models.Domain.Flow.History;
using Contingent.Utilities;
using Contingent.Utilities.Validation;
using Microsoft.AspNetCore.Routing.Tree;

namespace Contingent.Controllers;

public class GroupController : Controller
{

    private readonly ILogger<GroupController> _logger;

    public GroupController(ILogger<GroupController> logger)
    {
        _logger = logger;
    }

    [HttpGet]
    [Route("groups/modify/{query}")]
    public IActionResult ProcessGroup(string query)
    {
        if (query == "new")
        {
            return View(@"Views/Modify/GroupModify.cshtml", new GroupOutDTO());
        }
        else if (int.TryParse(query, out int id))
        {
            var got = GroupModel.GetGroupById(id, null);
            if (got == null)
            {
                return View(@"Views/Shared/Error.cshtml", "Группы с таким id не существует");
            }
            return View(@"Views/Modify/GroupModify.cshtml", got);
        }
        else
        {
            return View(@"Views/Shared/Error.cshtml", "Недопустимый id группы");
        }
    }
    [HttpGet]
    [Route("groups/view/{query}")]
    public IActionResult ViewGroup(string query)
    {
        if (int.TryParse(query, out int id))
        {
            var got = GroupModel.GetGroupById(id, null);
            if (got == null)
            {
                return View(@"Views/Shared/Error.cshtml", "Группы с таким id не существует");
            }
            return View(@"Views/Observe/Group.cshtml", new GroupOutDTO(got));
        }
        else
        {
            return View(@"Views/Shared/Error.cshtml", "Недопустимый id группы");
        }
    }


    [HttpPost]
    [Route("/groups/add")]
    public IActionResult Save()
    {
        using var reader = new StreamReader(Request.Body);
        var body = reader.ReadToEndAsync().Result;
        GroupInDTO? deserialized;
        try
        {
            deserialized = JsonSerializer.Deserialize<GroupInDTO>(body);
        }
        catch (Exception e)
        {
            return BadRequest(ErrorCollectionDTO.GetGeneralError("Неверный формат JSON: " + e.Message));
        }
        using var scope = ObservableTransaction.New;
        var groupResult = GroupModel.Build(deserialized, scope);
        if (groupResult.IsFailure)
        {
            scope.RollbackAsync().Wait();
            return BadRequest(groupResult.Errors.AsErrorCollection());
        }
        var group = groupResult.ResultObject;
        var saved = group.Save(null);
        if (saved.IsSuccess)
        {
            scope.CommitAsync().Wait();
            return Json(new GroupOutDTO(group));
        }
        else
        {
            scope.RollbackAsync().Wait();
            return BadRequest(saved.Errors.AsErrorCollection());
        }
    }
    [HttpPost]
    [Route("/groups/getname")]
    public IActionResult GenerateName()
    {
        using var reader = new StreamReader(Request.Body);
        var body = reader.ReadToEndAsync().Result;
        GroupInDTO? deserialized;
        try
        {
            deserialized = JsonSerializer.Deserialize<GroupInDTO>(body);
        }
        catch (Exception e)
        {
            return BadRequest(ErrorCollectionDTO.GetGeneralError("Неверный формат JSON: " + e.Message));
        }
        using var scope = ObservableTransaction.New;
        var groupResult = GroupModel.Build(deserialized, scope);
        if (groupResult.IsFailure)
        {
            scope.RollbackAsync().Wait();
            return BadRequest(groupResult.Errors.AsErrorCollection());
        }
        scope.RollbackAsync().Wait();
        return Json(new { GroupName = groupResult.ResultObject.ThreadNames });
    }

    [HttpPost]
    [Route("/groups/history")]
    public async Task<IActionResult> GetHistory()
    {
        using var reader = new StreamReader(Request.Body);
        var body = await reader.ReadToEndAsync();
        GroupHistoryQueryDTO? deserialized;
        try
        {
            deserialized = JsonSerializer.Deserialize<GroupHistoryQueryDTO>(body);
        }
        catch (Exception e)
        {
            return BadRequest(ErrorCollectionDTO.GetGeneralError("Неверный формат JSON: " + e.Message));
        }
        if (deserialized is null)
        {
            return BadRequest(ErrorCollectionDTO.GetGeneralError("Неверный формат JSON"));
        }
        var groupResult = GroupModel.GetGroupById(deserialized.Id);
        if (groupResult is not null)
        {
            var groupHistory = new GroupHistory(groupResult);
            if (Utils.TryParseDate(deserialized.OnDate, out DateTime date))
            {
                var toReturn = groupHistory.GetStateOnDate(date);
                return Json(toReturn.Select(x => new InGroupRelation(x.Student, x.ByOrder)));
            }
        }
        return BadRequest(new ValidationError("history", "Валидация параметров запроса истории не увенчалась успехом").AsErrorCollection());
    }
}

