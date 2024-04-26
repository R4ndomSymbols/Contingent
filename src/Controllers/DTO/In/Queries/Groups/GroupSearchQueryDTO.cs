namespace StudentTracking.Controllers.DTO.In;
[Serializable]
public class GroupSearchQueryDTO {
    public GroupSearchQueryDTO(){
        GroupName = null;
    }

    public string? GroupName {get; set;}
}