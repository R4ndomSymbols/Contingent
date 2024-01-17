namespace StudentTracking.Controllers.DTO.Out;

public class StudentResponseDTO {

    public int StudentId {get; set;}
    public string StudentFullName {get; set;}
    public GroupResponseDTO? Group {get; set;}

    public StudentResponseDTO(){
        StudentFullName = "";
        Group = null;
    }

}