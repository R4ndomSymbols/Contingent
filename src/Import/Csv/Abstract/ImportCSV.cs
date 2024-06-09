using System.Data;
using System.Globalization;
using System.Text;
using Contingent.Utilities;

namespace Contingent.Import.CSV;

public abstract class ImportCSV
{
    protected Stream _dataSource;
    protected ObservableTransaction _scope;

    protected ImportCSV(Stream dataSource, ObservableTransaction scope)
    {
        if (dataSource is null)
        {
            throw new ArgumentNullException(nameof(dataSource));
        }
        if (scope is null)
        {
            throw new ArgumentNullException(nameof(scope));
        }
        _dataSource = dataSource;
        _scope = scope;
    }

    protected Result<IEnumerable<T>> Read<T>(Func<T> factory, out List<CSVRow> rows) where T : IFromCSV<T>
    {
        int length = (int)_dataSource.Length;
        byte[] buffer = new byte[length];
        _dataSource.Read(buffer, 0, length);
        rows = new List<CSVRow>();
        string csv = Encoding.UTF8.GetString(buffer);
        if (csv.Length != new StringInfo(csv).LengthInTextElements)
        {
            return Result<IEnumerable<T>>.Failure(new ImportValidationError("CSV файл содержит недопустимые символы"));
        }
        var header = ReadHeader(csv, out int end);
        Console.WriteLine(header);
        var row = new StringBuilder();
        while (csv.Length > end)
        {
            if (csv[end] == '\n')
            {
                var parsed = CSVRow.Parse(header, row.ToString(), rows.Count + 1);
                if (parsed is null)
                {
                    return Result<IEnumerable<T>>.Failure(new ImportValidationError(string.Format("Строка {0} файле не соответствуют формату", rows.Count + 1)));
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
                return Result<IEnumerable<T>>.Failure(result.GetErrors().Append(new ImportValidationError("ОШИБКА НА СТРОКЕ " + csvRow.LineNumber)));
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
    protected void FinishImport(bool commit)
    {
        if (commit)
        {
            _scope.CommitAsync().Wait();
        }
        else
        {
            _scope.RollbackAsync().Wait();
        }
    }

    // импорт в объект, без сохранения
    public abstract ResultWithoutValue Import();
    // сохранение объектов
    public abstract ResultWithoutValue Save(bool commit);


}
