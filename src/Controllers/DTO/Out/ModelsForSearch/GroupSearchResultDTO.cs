using StudentTracking.Models.Domain.Groups;

namespace StudentTracking.Controllers.DTO.Out;

[Serializable]
public class GroupSearchResultDTO
{

    public GroupSearchResultDTO(GroupModel model)
    {

        if (model is null)
        {
            throw new ArgumentNullException("Группа должна быть указана при создании карточки поиска");
        }
        GroupId = model.Id;
        CourseOn = model.CourseOn.ToString();
        GroupName = model.GroupName;
        IsNameGenerated = model.IsNameGenerated;
        LinkToView = "/groups/view/" + GroupId.Value.ToString();
    }

    public int? GroupId { get; init; }
    public string CourseOn { get; init; }
    public string GroupName { get; init; }
    public bool? IsNameGenerated { get; init; }
    public string LinkToView { get; init; }

}
