using StudentTracking.Models.Domain.Orders;

namespace StudentTracking.Models.Domain.Flow;

// представляет собой запись в таблице движения студентов
// является агрегатом 

public class StudentFlowRecord {


    public RawStudentFlowRecord Record {get; private init;} 
    public Order? ByOrder {get; private init; }
    public StudentModel? Student {get; private init;}
    public GroupModel? GroupTo {get; private init; }

    public StudentFlowRecord(Order order, StudentModel student, GroupModel? group){
        if (order is null || student is null){
            throw new ArgumentNullException("Один из параметров записи истории не указан");
        }
        ByOrder = order;
        Student = student;
        GroupTo = group;
    }
    public StudentFlowRecord(RawStudentFlowRecord raw, Order? order, StudentModel? student, GroupModel? group){
        Record = raw;
        ByOrder = order;
        Student = student;
        GroupTo = group;
    }



} 


public struct RawStudentFlowRecord {
    public int? Id;
    public int? StudentId;
    public int? GroupToId;
    public int? OrderId;
}