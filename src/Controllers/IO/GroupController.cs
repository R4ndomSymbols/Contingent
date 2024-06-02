using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Contingent.Controllers.DTO.In;
using Contingent.Controllers.DTO.Out;
using Contingent.Models.Domain.Groups;
using Contingent.Models.Domain.Students;
using Contingent.Models.Domain.Flow.History;
using Utilities;
using Utilities.Validation;

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
            return BadRequest(ErrorCollectionDTO.GetGeneralError("Неверный формат JSON"));
        }
        var groupResult = GroupModel.Build(deserialized);
        if (groupResult.IsFailure)
        {
            return BadRequest(groupResult.Errors.AsErrorCollection());
        }
        var group = groupResult.ResultObject;
        var saved = group.Save(null);
        if (saved.IsSuccess)
        {
            return Json(new GroupOutDTO(group));
        }
        else
        {
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
            return BadRequest(ErrorCollectionDTO.GetGeneralError("Неверный формат JSON"));
        }
        var groupResult = GroupModel.Build(deserialized);

        if (groupResult.IsFailure)
        {
            return BadRequest(groupResult.Errors.AsErrorCollection());
        }
        return Json(new { GroupName = string.Join(", ", groupResult.ResultObject.GenerateGroupSequence().Select(x => x.GroupName)) });
    }

    [HttpPost]
    [Route("/groups/history")]
    public IActionResult GetHistory()
    {
        using var reader = new StreamReader(Request.Body);
        var body = reader.ReadToEnd();
        GroupHistoryQueryDTO? deserialized;
        try
        {
            deserialized = JsonSerializer.Deserialize<GroupHistoryQueryDTO>(body);
        }
        catch (Exception e)
        {
            return BadRequest(ErrorCollectionDTO.GetGeneralError("Неверный формат JSON"));
        }
        if (deserialized is null)
        {
            return BadRequest(ErrorCollectionDTO.GetGeneralError("Неверный формат JSON"));
        }
        var groupResult = GroupModel.GetGroupById(deserialized.Id);
        if (groupResult is not null)
        {
            var groupHistory = new GroupHistory(groupResult);
            if (Utils.TryParseDate(deserialized.OnDate))
            {
                var date = Utils.ParseDate(deserialized.OnDate);
                var toReturn = groupHistory.GetStateOnDate(date);
                return Json(toReturn.Select(x => new InGroupRelation(x.Student, x.ByOrder)));
            }
        }
        return BadRequest(new ValidationError("history", "Валидация параметров запроса истории не увенчалась успехом").AsErrorCollection());
    }
}

