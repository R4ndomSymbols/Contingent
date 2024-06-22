using Contingent.Models.Domain.Groups;
using Contingent.Models.Domain.Students;
using Contingent.Models.Domain.Orders;
using Contingent.Utilities;
using Contingent.Models.Infrastructure;
using System.Text.Json.Serialization;

namespace Contingent.Controllers.DTO.Out;
[Serializable]
public sealed class StudentHistoryMoveDTO
{

    public int StudentId { get; init; }
    public string StudentFullName { get; init; }
    public int? GroupIdTo { get; init; }
    public string GroupNameTo { get; init; }
    public int? GroupIdFrom { get; init; }
    public string GroupNameFrom { get; init; }
    public string GradeBookNumber { get; init; }
    public string OrderSpecifiedDate { get; init; }
    public string OrderOrgId { get; init; }
    public string OrderRussianTypeName { get; init; }
    public string StartDate { get; init; }
    public string EndDate { get; init; }
    [JsonIgnore]
    public Period? StatePeriod { get; init; }

    public StudentHistoryMoveDTO(StudentModel student, GroupModel? byOrderNow, GroupModel? previous, Order moveByThisOrder, Period? statePeriod = null)
    {
        StudentId = (int)student.Id!;
        StudentFullName = student.GetName();
        GroupIdTo = byOrderNow?.Id;
        GroupNameTo = byOrderNow?.GroupName ?? "Нет";
        GroupIdFrom = previous?.Id;
        GroupNameFrom = previous?.GroupName ?? "Нет";
        GradeBookNumber = student.GradeBookNumber;
        OrderOrgId = moveByThisOrder.OrderOrgId;
        OrderSpecifiedDate = Utils.FormatDateTime(moveByThisOrder.SpecifiedDate);
        OrderRussianTypeName = moveByThisOrder.GetOrderTypeDetails().OrderTypeName;
        StatePeriod = statePeriod;
        StartDate = Utils.FormatDateTime(statePeriod?.Start);
        EndDate = Utils.FormatDateTime(statePeriod?.End);
    }
}