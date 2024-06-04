using Contingent.Models.Domain.Groups;

namespace Contingent.Controllers.DTO.Out;

[Serializable]
public class GroupSearchResultDTO
{

    public GroupSearchResultDTO(GroupModel model)
    {

        if (model is null)
        {
            throw new ArgumentNullException("Группа должна быть указана при создании карточки поиска");
        }
        SpecialtyId = (int)model.EducationProgram.Id!;
        GroupId = model.Id;
        CourseOn = model.CourseOn.ToString();
        GroupName = model.GroupName;
        IsNameGenerated = model.IsNameGenerated;
        LinkToView = "/groups/view/" + GroupId.Value.ToString();
        SponsorshipTypeCode = (int)model.SponsorshipType.TypeOfSponsorship;
        EduFormatCode = (int)model.FormatOfEducation.FormatType;
        CreationYear = model.CreationYear;
    }
    public int SpecialtyId { get; set; }
    public int SponsorshipTypeCode { get; set; }
    public int EduFormatCode { get; set; }
    public int CreationYear { get; set; }
    public int? GroupId { get; init; }
    public string CourseOn { get; init; }
    public string GroupName { get; init; }
    public bool? IsNameGenerated { get; init; }
    public string LinkToView { get; init; }

}
