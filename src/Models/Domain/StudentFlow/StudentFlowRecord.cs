using Contingent.Models.Domain.Orders;
using Contingent.Models.Domain.Groups;
using Contingent.Models.Domain.Students;

namespace Contingent.Models.Domain.Flow;

// представляет собой запись в таблице движения студентов
// является агрегатом 

public class StudentFlowRecord
{


    public RawStudentFlowRecord Record { get; private init; }
    public Order? ByOrder { get; private init; }
    public StudentModel? Student { get; private init; }
    public GroupModel? GroupTo { get; private init; }
    public Order OrderNullRestrict => ByOrder is null ? throw new Exception("Приказ должен быть указан") : ByOrder;
    public StudentModel StudentNullRestrict => Student is null ? throw new Exception("Студент должен быть указан") : Student;
    public GroupModel GroupToNullRestrict => GroupTo is null ? throw new Exception("Группа должен быть указана") : GroupTo;

    public StudentFlowRecord(Order order, StudentModel student, GroupModel? group)
    {
        if (order is null || student is null)
        {
            throw new ArgumentNullException("Один из параметров записи истории не указан");
        }
        ByOrder = order;
        Student = student;
        GroupTo = group;
    }
    public StudentFlowRecord(RawStudentFlowRecord raw, Order? order, StudentModel? student, GroupModel? group)
    {
        Record = raw;
        ByOrder = order;
        Student = student;
        GroupTo = group;
    }



}


public struct RawStudentFlowRecord
{
    public int? Id;
    public int? StudentId;
    public int? GroupToId;
    public int? OrderId;
    public DateTime Timestamp;
}