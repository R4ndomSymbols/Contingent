using StudentTracking.Import;
using StudentTracking.Import.Concrete;
using System.ComponentModel;
using System.Text;
using Utilities;
namespace Tests;


public class Tests
{
    //[Fact]
    private void GenerateTable()
    {
        var csv = new CSVGenerator().GenerateStudents(100);
        Console.WriteLine(csv);
        var result = ImportCSV<StudentImport>.Read(new MemoryStream(Encoding.UTF8.GetBytes(csv)));
        Console.WriteLine(result);
        foreach (var student in result.ResultObject)
        {
            var saved = student.Student!.Save().Result;
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
        var result = ImportCSV<OrderImport>.Read(new MemoryStream(Encoding.UTF8.GetBytes(csv)));
        Console.WriteLine(result);
        if (result.IsSuccess)
        {
            foreach (var order in result.ResultObject)
            {
                order.ImportedOrder!.Save();
            }
        }
    }
    [Fact]
    public void GenerateTable2()
    {
        var csv = new CSVGenerator().GetSpecialities();
        Console.WriteLine(csv);
        var result = ImportCSV<SpecialityImport>.Read(new MemoryStream(Encoding.UTF8.GetBytes(csv)));
        Console.WriteLine(result);
        if (result.IsSuccess)
        {
            foreach (var speciality in result.ResultObject)
            {
                speciality.Speciality!.Save();
            }
        }
    }

}

// moving to branch dev