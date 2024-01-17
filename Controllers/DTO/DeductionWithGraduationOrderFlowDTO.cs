using System.Text.Json.Serialization;
using StudentTracking.Models.Domain.Orders;
using StudentTracking.Models.Domain.Orders.OrderData;

namespace StudentTracking.Controllers.DTO;

[Serializable]
public class DeductionWithGraduationOrderFlowDTO {

    [JsonRequired]
    public List<int> Students {get; set; }

    public DeductionWithGraduationOrderFlowDTO(){
        Students = new List<int>();
    }
}
