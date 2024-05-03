using Utilities;

namespace Contingent.Import;

public interface IFromCSV<T>
{
    public Result<T> MapFromCSV(CSVRow row);
}