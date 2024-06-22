using Contingent.Models;
using Contingent.Models.Domain.Students;
using Contingent.Models.Domain.Groups;
using Contingent.Models.Domain.Orders;
using Contingent.SQL;
using Contingent.Import;
using Contingent.Statistics;
using Contingent.Controllers.DTO.In;

namespace Tests;

public class OrderConductionDataSource : IRowSource
{
    private static string[] _headers = new[] {
        "Индекс",
        StudentStatementDTO.GradeBookFieldName,
        StudentStatementDTO.StudentFullNameFieldName,
        GroupStatementDTO.GroupNameFieldName,
        OrderIdentityDTO.OrderOrgIdFieldName,
        OrderIdentityDTO.OrderSpecifiedYearFieldName,
        "Статус"
    };
    private string[] _data;
    private static List<StudentModel> students;
    private static List<GroupModel> groups;
    private static List<Order> orders;
    private OrderTypes _mode;
    private IList<StudentModel> _filteredStudents;
    private IList<GroupModel> _filteredGroups;
    private IList<Order> _filteredOrders;
    private int _index = 0;

    static OrderConductionDataSource()
    {
        students = new List<StudentModel>(StudentModel.FindStudents(new QueryLimits(0, 1000)).Result);
        groups = new List<GroupModel>(GroupModel.FindGroups(new QueryLimits(0, 1000)).Result);
        orders = new List<Order>(Order.FindOrders(new QueryLimits(0, 1000)).Result);
    }

    public OrderConductionDataSource(OrderTypes mode)
    {
        _data = new string[_headers.Length];
        _mode = mode;
        if (_mode == OrderTypes.FreeEnrollment)
        {
            _filteredStudents = new Filter<StudentModel>(
                (s) => s.Where(student => !student.PaidAgreement.IsConcluded() && !student.GetHistory(null).IsStudentEnlisted())
            ).Execute(students).ToList();
            _filteredGroups = new Filter<GroupModel>(
                (g) => g.Where(group => group.CourseOn == 1 && group.SponsorshipType.IsFree())
            ).Execute(groups).ToList();
            _filteredOrders = new Filter<Order>(
                (o) => o.Where(order => order.GetOrderTypeDetails().Type == OrderTypes.FreeEnrollment).Skip(new Random().Next(0, 10)).Take(1)
            ).Execute(orders).ToList();
        }
        else if (_mode == OrderTypes.FreeEnrollmentFromAnotherOrg)
        {
            _filteredStudents = new Filter<StudentModel>(
                (s) => s.Where(student => !student.PaidAgreement.IsConcluded() && !student.GetHistory(null).IsStudentEnlisted())
            ).Execute(students).ToList();
            _filteredGroups = new Filter<GroupModel>(
                (g) => g.Where(group => (group.CourseOn == 1 || group.CourseOn == 2) && group.SponsorshipType.IsFree())
            ).Execute(groups).ToList();
            _filteredOrders = new Filter<Order>(
                (o) => o.Where(order => order.GetOrderTypeDetails().Type == OrderTypes.FreeEnrollmentFromAnotherOrg)
            ).Execute(orders).ToList();
        }
        Console.WriteLine(
            string.Format(
                "{0} students, {1} groups, {2} orders", _filteredStudents!.Count, _filteredGroups!.Count, _filteredOrders!.Count
            )
        );
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
        _index++;
        var student = RandomPicker<StudentModel>.Pick(_filteredStudents);
        var group = RandomPicker<GroupModel>.Pick(_filteredGroups);
        var order = RandomPicker<Order>.Pick(_filteredOrders);
        _data[0] = _index.ToString();
        _data[1] = student.GradeBookNumber;
        _data[2] = student.GetName();
        _data[3] = group.GroupName;
        _data[4] = order.OrderOrgId;
        _data[5] = order.SpecifiedDate.Year.ToString();
        _data[6] = student.GetHistory(null).GetStudentState(out int cc).ToString();
    }
}
