using System.Text.Json.Serialization;
using StudentTracking.Models.Domain.Orders;
using StudentTracking.Models.Domain.Orders.OrderData;

namespace StudentTracking.Controllers.DTO.In;

[Serializable]
public class EnrollmentOrderFlowDTO {

    [JsonRequired]
    public List<StudentMoveDTO> Moves {get; set; }

    public EnrollmentOrderFlowDTO(){
        Moves = new List<StudentMoveDTO>();
    }
}