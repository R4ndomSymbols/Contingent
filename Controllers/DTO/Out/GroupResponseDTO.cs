using System.Text.RegularExpressions;
using StudentTracking.Models;

namespace StudentTracking.Controllers.DTO.Out;

[Serializable]
public class GroupResponseDTO {

    public int? GroupId {get; set;}
    public string GroupName {get; set;}
    public bool IsNameGenerated {get; set;}

    public GroupResponseDTO(GroupModel? model){
        GroupName = model?.GroupName ?? "Нет";
        GroupId = model?.Id;
        IsNameGenerated = true;
    }

}