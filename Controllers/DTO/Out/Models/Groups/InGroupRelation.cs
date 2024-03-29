using StudentTracking.Models;
using StudentTracking.Models.Domain;
using StudentTracking.Models.Domain.Orders;
using Utilities;

namespace StudentTracking.Controllers.DTO.Out;

[Serializable]
public class InGroupRelation {

    public InGroupRelation(StudentModel student, Order byOrder){
        StudentGradeBookNumber = student.GradeBookNumber;
        StudentName = student.GetName();
        OrderTypeName = byOrder.GetOrderTypeDetails().OrderTypeName;
        OrderLink = "/orders/view/" + byOrder.Id;
        OrderEffectiveDate = Utils.FormatDateTime(byOrder.EffectiveDate);
        StudentLink = "/students/view/" + student.Id;
    }

    public string StudentGradeBookNumber { get; set; }
    public string StudentName {get; set;}
    public string StudentLink {get; set;}
    public string OrderTypeName {get; set;}
    public string OrderLink {get; set;}
    public string OrderEffectiveDate {get; set;}
}