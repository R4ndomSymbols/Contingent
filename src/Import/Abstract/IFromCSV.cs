using Utilities;

namespace StudentTracking.Import;

public interface IFromCSV<T> 
{
    public Result<T> MapFromCSV(CSVRow row);
}