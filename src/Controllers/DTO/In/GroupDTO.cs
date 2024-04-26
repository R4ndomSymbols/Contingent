using System.Text.Json.Serialization;

namespace StudentTracking.Controllers.DTO.In;

[Serializable]
public sealed class GroupInDTO {
    
    [JsonRequired]
    public int EduProgramId {get; set;}
    [JsonRequired]
    public int EduFormatCode {get; set; }
    [JsonRequired]
    public int SponsorshipTypeCode {get; set;}
    [JsonRequired] 
    public string CreationYear {get; set; }
    [JsonRequired]
    public bool AutogenerateName {get; set;}
    public string GroupName {get; set;}

    public GroupInDTO(){
        
    }
}