using System.Data;
using System.Globalization;
using System.Text;
using Microsoft.VisualBasic;
using Utilities;

namespace Contingent.Import;

public class ImportCSV<T> where T : IFromCSV<T>
{

    public static Result<IEnumerable<T>> Read(Stream dataSource, Func<T> factory)
    {
        int length = (int)dataSource.Length;
        byte[] buffer = new byte[length];
        dataSource.Read(buffer, 0, length);
        string csv = Encoding.UTF8.GetString(buffer);
        if (csv.Length != new StringInfo(csv).LengthInTextElements)
        {
            return Result<IEnumerable<T>>.Failure(new ValidationError("import", "CSV файл содержит недопустимые символы"));
        }
        var header = ReadHeader(csv, out int end);
        Console.WriteLine(header);
        var rows = new List<CSVRow>();
        var row = new StringBuilder();
        while (csv.Length > end)
        {
            if (csv[end] == '\n')
            {
                var parsed = CSVRow.Parse(header, row.ToString(), rows.Count + 1);
                if (parsed is null)
                {
                    return Result<IEnumerable<T>>.Failure(new ValidationError(string.Format("Строка {0} файле не соответствуют формату", rows.Count + 1)));
                }
                rows.Add(parsed);
                row.Clear();
            }
            else
            {
                row.Append(csv[end]);
            }
            end++;
        }
        var results = new List<T>();
        foreach (var csvRow in rows)
        {
            var result = factory.Invoke().MapFromCSV(csvRow);
            if (result.IsFailure)
            {
                return Result<IEnumerable<T>>.Failure(result.GetErrors().Append(new ValidationError("ОШИБКА НА СТРОКЕ " + csvRow.LineNumber)));
            }
            else
            {
                results.Add(result.ResultObject);
            }
        }
        return Result<IEnumerable<T>>.Success(results);
    }

    private static CSVHeader ReadHeader(string csv, out int offset)
    {
        string current = string.Empty;
        CSVHeader header = new CSVHeader();
        int columnIndex = 0;
        offset = 0;
        for (; offset < csv.Length && csv[offset] != '\n'; offset++)
        {
            if (csv[offset] == ',')
            {
                header.AddColumn(columnIndex, current.Trim());
                columnIndex++;
                current = string.Empty;
            }
            else
            {
                current += csv[offset];
            }
        }
        offset++;
        header.AddColumn(columnIndex, current.Trim());
        return header;
    }



}
