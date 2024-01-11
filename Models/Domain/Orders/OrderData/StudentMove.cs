using Microsoft.AspNetCore.Mvc.ModelBinding;
using Utilities.Validation;

namespace StudentTracking.Models.Domain.Orders.OrderData;


public class StudentMove : ValidatedObject 
{
    private int _studentId;
    private int _groupToId;
    public int StudentId {
        get => _studentId; 
        set {
            bool exists = StudentModel.IsIdExists(value, null).Result;
            if (PerformValidation(
                () => exists, new ValidationError(nameof(StudentId), "invalid")
            )){
                _studentId = value;
            }
        } 
    }
    public int GroupToId {
        get => _groupToId; 
        set {
            bool exists = GroupModel.IsIdExists(value, null).Result;
            if (PerformValidation(
                () => exists, new ValidationError(nameof(GroupToId), "invalid")
            )){
                _groupToId = value;
            }
        } 
    }
    public StudentMove(){
        
    }
}

public struct StudentStatement {
    public readonly int StudentId;

    public StudentStatement(int studentId){
        StudentId = studentId;
    }
}
