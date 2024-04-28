using StudentTracking.Models;
using StudentTracking.Models.Domain.Misc;

namespace StudentTracking.Controllers.DTO.Out;

public sealed class GroupOutDTO {
    public int? Id {get; set;}
    public int CourseOn {get; set; }
    public int EduFormatCode {get; set; }
    public string EduFormatName {get; set;}
    public int SponsorshipTypeCode {get; set;}
    public string SponsorshipTypeName {get; set;}
    public string CreationYear {get; set; }
    public bool AutogenerateName {get; set;}
    public string GroupName {get; set;}
    public SpecialityOutDTO Speciality {get; set;}
    public GroupOutDTO(GroupModel model){
        Id = model.Id; 
        GroupName = model.GroupName;
        EduFormatName = model.FormatOfEducation.RussianName;
        SponsorshipTypeName = model.SponsorshipType.RussianName;
        Speciality = new  SpecialityOutDTO(model.EducationProgram);
        EduFormatCode = (int)model.FormatOfEducation.FormatType;
        SponsorshipTypeCode = (int)model.SponsorshipType.TypeOfSponsorship;
        AutogenerateName = model.IsNameGenerated;
        CreationYear = model.CreationYear.ToString();
        CourseOn = model.CourseOn;
    }
    public GroupOutDTO(){
        Id = null;
        EduFormatCode = (int)GroupEducationFormatTypes.NotMentioned;
        SponsorshipTypeCode = (int)GroupSponsorshipTypes.NotMentioned;
        CreationYear = "";
        AutogenerateName = true;
        GroupName = "";
        Speciality = new SpecialityOutDTO();
        SponsorshipTypeName = "";
        EduFormatName = "";
        CourseOn=0;
    }
}