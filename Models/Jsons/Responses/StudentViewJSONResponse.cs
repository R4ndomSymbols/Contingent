namespace StudentTracking.Models.JSON.Responses;

public class StudentViewJSONResponse {

    public int StudentId {get; set;}
    public string StudentFullName {get; set;}
    public GroupViewJSONResponse? Group {get; set;}

    public StudentViewJSONResponse(){
        StudentFullName = "";
        Group = null;
    }

}