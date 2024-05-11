using Contingent.Import;
using Contingent.Import.Concrete;
using Contingent.Models.Domain.Orders;
using Contingent.Models.Domain.Orders.OrderData;
using System.ComponentModel;
using System.Text;
using Utilities;
namespace Tests;


public class Tests
{
    public void GenerateTable()
    {
        var csv = new CSVGenerator().GenerateStudents(500);
        Console.WriteLine(csv);
        var result = ImportCSV<StudentImport>.Read(new MemoryStream(Encoding.UTF8.GetBytes(csv)), () => new StudentImport());
        Console.WriteLine(result);
        foreach (var student in result.ResultObject)
        {
            var saved = student.Student!.Save();
            if (saved.IsFailure)
            {
                Console.WriteLine(saved.Errors.First());
            };
        }
    }
    private void GenerateTable1()
    {
        var csv = new CSVGenerator().GenerateOrders(100);
        Console.WriteLine(csv);
        var result = ImportCSV<OrderImport>.Read(new MemoryStream(Encoding.UTF8.GetBytes(csv)), () => new OrderImport());
        Console.WriteLine(result);
        if (result.IsSuccess)
        {
            foreach (var order in result.ResultObject)
            {
                order.ImportedOrder!.Save();
            }
        }
    }
    private void GenerateTable2()
    {
        var csv = new CSVGenerator().GetSpecialities();
        Console.WriteLine(csv);
        var result = ImportCSV<SpecialityImport>.Read(new MemoryStream(Encoding.UTF8.GetBytes(csv)), () => new SpecialityImport());
        Console.WriteLine(result);
        if (result.IsSuccess)
        {
            foreach (var speciality in result.ResultObject)
            {
                speciality.Speciality!.Save();
            }
        }
    }
    public void GroupImportTest()
    {
        var csv = new CSVGenerator().GenerateGroups(30);
        Console.WriteLine(csv);
        var result = ImportCSV<GroupImport>.Read(new MemoryStream(Encoding.UTF8.GetBytes(csv)), () => new GroupImport());
        Console.WriteLine(result);
        if (result.IsSuccess)
        {
            foreach (var group in result.ResultObject)
            {
                group.Group!.Save();
            }
        }
    }

    [Fact]
    public void FlowImportTest()
    {
        var csv = new CSVGenerator().GenerateFlow(150, OrderTypes.FreeEnrollmentFromAnotherOrg);
        var batch = new FlowImportBatch();
        Console.WriteLine(csv);
        var result = ImportCSV<FlowImport>.Read(new MemoryStream(Encoding.UTF8.GetBytes(csv)), () => new FlowImport(batch));
        Console.WriteLine(result);
        if (result.IsSuccess)
        {
            batch.MassConduct();
        }
    }

    public void OrderTypesTest()
    {
        Console.WriteLine(
            string.Join(
                "\n",
                OrderTypeInfo.GetAllTypes().Select(x => x.Type.ToString() + " " + x.IsAnyDeduction().ToString() + " " + x.IsAnyEnrollment().ToString())
            )

        );
    }


}

// moving to branch dev