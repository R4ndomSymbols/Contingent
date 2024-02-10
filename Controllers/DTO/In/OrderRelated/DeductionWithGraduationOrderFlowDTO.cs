using System.Text.Json.Serialization;
using StudentTracking.Models.Domain.Orders;
using StudentTracking.Models.Domain.Orders.OrderData;

namespace StudentTracking.Controllers.DTO.In;

[Serializable]
public class StudentGroupNullifyFlowDTO {

    [JsonRequired]
    public List<int> Students {get; set; }

    public StudentGroupNullifyFlowDTO(){
        Students = new List<int>();
    }
}
