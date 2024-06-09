using System.Text;
using Contingent.Models.Domain.Orders;
using Contingent.Tests;

namespace Tests;

public class CSVGenerator
{

    private string GenerateBase(IDataSource[] data, int count)
    {
        var csv = new StringBuilder();
        csv.Append(string.Join(',', data.Select(x => x.Name)));
        csv.Append('\n');
        for (int i = 0; i < count; i++)
        {
            csv.Append(string.Join(',', data.Select(x => x.Value)));
            csv.Append('\n');
        }
        return csv.ToString();
    }
    private string GenerateBase(IRowSource data, int count)
    {
        var csv = new StringBuilder();
        csv.Append(string.Join(',', Enumerable.Range(0, data.ColumnCount).Select(x => data.GetHeader(x))));
        csv.Append('\n');
        for (int i = 0; i < count; i++)
        {
            csv.Append(string.Join(',', Enumerable.Range(0, data.ColumnCount).Select(x => data.GetData(x))));
            csv.Append('\n');
            data.UpdateState();
        }
        return csv.ToString();
    }

    public string GenerateStudents(int count)
    {
        var source = new IDataSource[]{
            StudentDataSource.GradeBook,
            StudentDataSource.AdmissionScore,
            StudentDataSource.DateOfBirth,
            StudentDataSource.Gender,
            StudentDataSource.Names,
            StudentDataSource.Surnames,
            StudentDataSource.Patronymics,
            StudentDataSource.Snils,
            StudentDataSource.PaidAgreement,
            StudentDataSource.TargetAgreement,
            StudentDataSource.Education,
        };
        return GenerateBase(source, count);
    }

    public string GenerateOrders(int count)
    {
        return GenerateBase(new OrderRowDataSource(), count);
    }
    public string GetSpecialties()
    {
        var source = new SpecialtyDataSource();
        return GenerateBase(source, source.Length);
    }
    public string GenerateFlow(int count, OrderTypes type)
    {
        var source = new OrderConductionDataSource(type);
        return GenerateBase(source, count);
    }
    public string GenerateGroups(int count)
    {
        var source = new GroupRowDataSource();
        return GenerateBase(source, count);
    }
}
