using Contingent.Auth;
using Contingent.Models.Domain.Orders;
using Contingent.Models.Domain.Orders.OrderData;
using System.Text;
using Contingent.Utilities;
using Contingent.Import.CSV;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;
using System.Security.Cryptography;
using Contingent.DTOs.In;
using Xunit.Abstractions;
namespace Tests;

public class ClientWConsole {
        protected readonly ITestOutputHelper output;

        public ClientWConsole(ITestOutputHelper output) {
            this.output = output;
        }

}
public class Tests : ClientWConsole
{
    public Tests(ITestOutputHelper output) : base(output) { }

    public void MakeImportFile()
    {
        var csv = new CSVGenerator().GenerateOrders(100);
        File.WriteAllText(@"/home/RandomSymbols/fhdd/projects/Contingent/docs/test.csv", csv);
    }
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
}

// moving to branch dev