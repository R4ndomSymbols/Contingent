using System.Security.Cryptography.X509Certificates;

namespace Contingent.Import;

public class CSVRow
{

    private string[] _line;
    private CSVHeader _header;

    public readonly int LineNumber;

    private CSVRow(int lineNumber, string[] line, CSVHeader header)
    {
        LineNumber = lineNumber;
        _line = line;
        _header = header;
    }

    public static CSVRow? Parse(CSVHeader header, string row, int lineNumber)
    {
        string[] parts = row.Split(',');
        if (parts.Length != header.ColumnCount)
        {
            return null;
        }
        return new CSVRow(lineNumber, parts, header);
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
