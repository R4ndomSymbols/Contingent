using System.Security.Cryptography.X509Certificates;

namespace Contingent.Import;

public class CSVRow
{

    private string[] _line;
    private CSVHeader _header;

    public readonly int LineNumber;

    private CSVRow(int lineNumber)
    {
        LineNumber = lineNumber;
    }

    public static CSVRow? Parse(CSVHeader header, string row, int lineNumber)
    {
        var csvLine = new CSVRow(lineNumber);
        string[] parts = row.Split(',');
        if (parts.Length != header.ColumnCount)
        {
            return null;
        }
        csvLine._line = parts;
        csvLine._header = header;
        return csvLine;
    }

    public string? this[string name]
    {
        get
        {
            int index = _header[name];
            if (index == -1)
            {
                return null;
            }
            return _line[index];
        }
    }



}
