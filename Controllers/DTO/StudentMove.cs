using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Utilities.Validation;

namespace StudentTracking.Controllers.DTO;

[Serializable]
public class StudentMoveDTO 
{
    [JsonRequired]
    public int StudentId { get; set; }
    [JsonRequired]
    public int GroupToId { get; set; }
    public StudentMoveDTO(){
        
    }
}
