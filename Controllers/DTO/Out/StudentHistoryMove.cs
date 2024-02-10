using StudentTracking.Models;
using StudentTracking.Models.Domain;
using StudentTracking.Models.Domain.Orders;
using StudentTracking.Models.SQL;
using Utilities;

namespace StudentTracking.Controllers.DTO.Out;
[Serializable]
public sealed class StudentHistoryMoveDTO {

    public int StudentId {get; init;}
    public string StudentFullName {get; init; }
    public int? GroupIdTo {get; init; }
    public string GroupNameTo {get; init; }
    public int? GroupIdFrom {get; init; }
    public string GroupNameFrom {get; init; }
    public string GradeBookNumber {get; init;}
    public string OrderSpecifiedDate {get; init;}
    public string OrderOrgId {get; init;}
    public string OrderRussianTypeName {get; init;}

    public StudentHistoryMoveDTO(StudentModel student, GroupModel? byOrderNow, GroupModel? previous, Order moveByThisOrder ){
        StudentId = student.Id;
        StudentFullName = student.GetName().Result;
        GroupIdTo = byOrderNow?.Id;
        GroupNameTo = byOrderNow?.GroupName ?? "Нет"; 
        GroupIdFrom = previous?.Id;
        GroupNameFrom = previous?.GroupName ?? "Нет"; 
        GradeBookNumber = student.GradeBookNumber;
        OrderOrgId = moveByThisOrder.OrderOrgId;
        OrderSpecifiedDate = Utils.FormatDateTime(moveByThisOrder.SpecifiedDate);
        OrderRussianTypeName = moveByThisOrder.GetOrderTypeDetails().OrderTypeName;
    }

}