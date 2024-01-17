namespace StudentTracking.Controllers.DTO.Out;

[Serializable]
public class GroupResponseDTO {

    public int? GroupId {get; set;}
    public string GroupName {get; set;}
    public bool IsNameGenerated {get; set;}

    public GroupResponseDTO(){
        GroupName = "Нет";
        GroupId = null;
        IsNameGenerated = true;
    }

}