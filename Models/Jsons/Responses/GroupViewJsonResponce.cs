namespace StudentTracking.Models.JSON.Responses;

[Serializable]
public class GroupViewJSONResponse {

    public int? GroupId {get; set;}
    public string GroupName {get; set;}
    public bool IsNameGenerated {get; set;}

    public GroupViewJSONResponse(){
        GroupName = "Нет";
        GroupId = null;
        IsNameGenerated = true;
    }

}