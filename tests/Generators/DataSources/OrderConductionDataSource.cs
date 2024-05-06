using Contingent.Models;
using Contingent.Models.Domain.Students;
using Contingent.Models.Domain.Groups;
using Contingent.Models.Domain.Orders;
using Contingent.SQL;
using Contingent.Import;
using Contingent.Statistics;

namespace Tests;

public class OrderConductionDataSource : IRowSource
{
    private static string[] _headers = new[] {
        FlowImport.GradeBookFieldName,
        FlowImport.StudentFullNameFieldName,
        FlowImport.GroupFieldName,
        FlowImport.OrderFieldOrgIdName,
        FlowImport.OrderFieldDateName
    };
    private string[] _data;
    private static List<StudentModel> students;
    private static List<GroupModel> groups;
    private static List<Order> orders;
    private OrderTypes _mode;
    private IList<StudentModel> _filtedStudents;
    private IList<GroupModel> _filtedGroups;
    private IList<Order> _filtedOrders;

    static OrderConductionDataSource()
    {
        students = new List<StudentModel>(StudentModel.FindUniqueStudents(new QueryLimits(0, 1000)).Result);
        groups = new List<GroupModel>(GroupModel.FindGroups(new QueryLimits(0, 1000)).Result);
        orders = new List<Order>(Order.FindOrders(new QueryLimits(0, 1000)).Result);
    }

    public OrderConductionDataSource(OrderTypes mode)
    {
        _data = new string[_headers.Length];
        _mode = mode;
        if (_mode == OrderTypes.FreeEnrollment)
        {
            _filtedStudents = new Filter<StudentModel>(
                (s) => s.Where(student => !student.PaidAgreement.IsConcluded())
            ).Execute(students).ToList();
            _filtedGroups = new Filter<GroupModel>(
                (g) => g.Where(group => group.CourseOn == 1)
            ).Execute(groups).ToList();
            _filtedOrders = new Filter<Order>(
                (o) => o.Where(order => order.GetOrderTypeDetails().Type == OrderTypes.FreeEnrollment)
            ).Execute(orders).ToList();
        }
        else if (_mode == OrderTypes.PaidEnrollment)
        {
            _filtedStudents = new Filter<StudentModel>(
                (s) => s.Where(student => student.PaidAgreement.IsConcluded())
            ).Execute(students).ToList();
            _filtedGroups = new Filter<GroupModel>(
                (g) => g.Where(group => group.CourseOn == 1 && group.SponsorshipType.IsPaid())
            ).Execute(groups).ToList();
        }
        UpdateState();

    }

    public int ColumnCount => _headers.Length;

    public string GetData(int pos)
    {
        return _data[pos];
    }

    public string? GetHeader(int pos)
    {
        return _headers[pos];
    }

    public void UpdateState()
    {
        _data = new string[_headers.Length];
        var student = RandomPicker<StudentModel>.Pick(_filtedStudents);
        var group = RandomPicker<GroupModel>.Pick(_filtedGroups);
        var order = RandomPicker<Order>.Pick(_filtedOrders);
        _data[0] = student.GradeBookNumber;
        _data[1] = student.GetName();
        _data[2] = group.GroupName;
        _data[3] = order.OrderOrgId;
        _data[4] = order.SpecifiedDate.Year.ToString();
    }
}
