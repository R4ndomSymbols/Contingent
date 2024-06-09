using Contingent.Import;
using Contingent.Models.Domain.Orders;
using Contingent.Models.Domain.Orders.OrderData;
using System.Text;
using Contingent.Utilities;
using Contingent.Import.CSV;
namespace Tests;


public class Tests
{
    public void GenerateStudents()
    {
        var csv = new CSVGenerator().GenerateStudents(10000);
        Console.WriteLine(csv);
        using var transaction = ObservableTransaction.New;
        var import = new StudentImport(new MemoryStream(Encoding.UTF8.GetBytes(csv)), transaction);
        var importResult = import.Import();
        if (importResult.IsFailure)
        {
            Console.WriteLine(importResult);
            return;
        }
        var saveResult = import.Save(true);
        if (saveResult.IsFailure)
        {
            Console.WriteLine(saveResult);
            return;
        }
    }
    public void GenerateSpecialities()
    {
        var csv = new CSVGenerator().GetSpecialties();
        Console.WriteLine(csv);
        using var transaction = ObservableTransaction.New;
        var import = new SpecialtyImport(new MemoryStream(Encoding.UTF8.GetBytes(csv)), transaction);
        var importResult = import.Import();
        if (importResult.IsFailure)
        {
            Console.WriteLine(importResult);
            return;
        }
        var saveResult = import.Save(true);
        if (saveResult.IsFailure)
        {
            Console.WriteLine(saveResult);
            return;
        }

    }
    private void GenerateGroups()
    {
        var csv = new CSVGenerator().GenerateGroups(100);
        Console.WriteLine(csv);
        using var transaction = ObservableTransaction.New;
        var import = new GroupImport(new MemoryStream(Encoding.UTF8.GetBytes(csv)), transaction);
        var importResult = import.Import();
        if (importResult.IsFailure)
        {
            Console.WriteLine(importResult);
            return;
        }
        var saveResult = import.Save(true);
        if (saveResult.IsFailure)
        {
            Console.WriteLine(saveResult);
            return;
        }

    }
    public void GenerateOrdersTest()
    {
        var csv = new CSVGenerator().GenerateOrders(100);
        Console.WriteLine(csv);
        using var transaction = ObservableTransaction.New;
        var import = new OrderImport(new MemoryStream(Encoding.UTF8.GetBytes(csv)), transaction);
        var importResult = import.Import();
        if (importResult.IsFailure)
        {
            Console.WriteLine(importResult);
            return;
        }
        var saveResult = import.Save(true);
        if (saveResult.IsFailure)
        {
            Console.WriteLine(saveResult);
            return;
        }
    }
    [Fact]
    public void FlowImportTest()
    {
        var csv = new CSVGenerator().GenerateFlow(100, OrderTypes.FreeEnrollment);
        Console.WriteLine(csv);
        using var transaction = ObservableTransaction.New;
        var import = new FlowImport(new MemoryStream(Encoding.UTF8.GetBytes(csv)), transaction);
        import.CloseOrders = true;
        var importResult = import.Import();
        if (importResult.IsFailure)
        {
            Console.WriteLine(importResult);
            return;
        }
        var saveResult = import.Save(true);
        if (saveResult.IsFailure)
        {
            Console.WriteLine(saveResult);
            return;
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