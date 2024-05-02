using StudentTracking.Models;
using StudentTracking.Models.Domain.Students;
using StudentTracking.Models.Domain.Groups;
using StudentTracking.Models.Domain.Orders;
using StudentTracking.SQL;

namespace Tests;

public class OrderConductionDataSource
{

    public static List<StudentModel> students;
    public static List<GroupModel> groups;
    public static List<Order> orders;

    static OrderConductionDataSource()
    {
        students = new List<StudentModel>(StudentModel.FindUniqueStudents(new QueryLimits(0, 1000)).Result);
        groups = new List<GroupModel>();
        orders = new List<Order>();
    }

    public OrderConductionDataSource()
    {

    }



}