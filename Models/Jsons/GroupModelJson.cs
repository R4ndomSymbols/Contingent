using Microsoft.AspNetCore.Components.Forms;
using Npgsql.PostgresTypes;

namespace StudentTracking.Models.JSON;

[Serializable]
public sealed class GroupModelJSON {

    public int Id {get; set; }
    public int EduProgramId {get; set;}
    public int EduFormatCode {get; set; }
    public int SponsorshipTypeCode {get; set;} 
    public string CreationYear {get; set; }
    public bool AutogenerateName {get; set;}
    public string GroupName {get; set;}

    public GroupModelJSON(){
        GroupName = "";
        CreationYear = "";
    }
}
