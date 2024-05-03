using Contingent.Models.Domain.Students;
using Contingent.Models.Domain.Orders;
using Utilities;

namespace Contingent.Controllers.DTO.Out;

[Serializable]
public class InGroupRelation
{

    public InGroupRelation(StudentModel? student, Order? byOrder)
    {
        if (student is null || byOrder is null)
        {
            throw new ArgumentNullException(nameof(student), "Студент и приказ должны быть указаны при создании карточки истории");
        }
        StudentGradeBookNumber = student.GradeBookNumber;
        StudentName = student.GetName();
        OrderTypeName = byOrder.GetOrderTypeDetails().OrderTypeName;
        OrderLink = "/orders/view/" + byOrder.Id;
        OrderEffectiveDate = Utils.FormatDateTime(byOrder.EffectiveDate);
        StudentLink = "/students/view/" + student.Id;
        OrderOrgId = byOrder.OrderOrgId;
    }

    public string StudentGradeBookNumber { get; set; }
    public string StudentName { get; set; }
    public string StudentLink { get; set; }
    public string OrderTypeName { get; set; }
    public string OrderLink { get; set; }
    public string OrderEffectiveDate { get; set; }
    public string OrderOrgId { get; set; }
}