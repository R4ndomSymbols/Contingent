using System.Text.Json.Serialization;
using StudentTracking.Models.Domain.Orders;
using StudentTracking.Models.Domain.Orders.OrderData;

namespace StudentTracking.Controllers.DTO.In;

[Serializable]
public class TransferGroupToGroupOrderFlowDTO {

    [JsonRequired]
    public List<StudentMoveDTO> Moves {get; set; }

    public TransferGroupToGroupOrderFlowDTO(){
        Moves = new List<StudentMoveDTO>();
    }
}
