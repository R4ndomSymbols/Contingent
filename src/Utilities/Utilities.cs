using System.Globalization;
using Npgsql;

namespace Contingent.Utilities;


public static class Utils
{

    public const int INVALID_ID = -1;

    public static bool IsValidId(int id)
    {
        return id != INVALID_ID;
    }
    public static bool IsValidId(int? id)
    {
        return id != null && IsValidId(id.Value);
    }


    public static string FormatDateTime(DateTime? date, bool expand = false)
    {
        if (date is null)
        {
            return "";
        }
        var correctDate = (DateTime)date;
        if (expand)
        {
            return string.Format("{0} {1} {2}",
            correctDate.Day,
            correctDate.ToString("MMMM", CultureInfo.CreateSpecificCulture("ru")),
            correctDate.Year.ToString() + " года");
        }
        return correctDate.ToString("dd.MM.yyyy");
    }
    public static bool TryParseDate(string? date, out DateTime parsed)
    {
        parsed = DateTime.MinValue;
        if (string.IsNullOrEmpty(date))
        {
            return false;
        }
        else
        {
            var parts = date.Split('.');
            if (parts.Length != 3)
            {
                return false;
            }
            if (int.TryParse(parts[0], out int day))
            {
                if (int.TryParse(parts[1], out int month))
                {
                    if (int.TryParse(parts[2], out int year))
                    {
                        try
                        {
                            parsed = new DateTime(year, month, day);
                            return true;
                        }
                        catch (ArgumentOutOfRangeException)
                        {
                            return false;
                        }
                    }
                    return false;
                }
                return false;
            }
            return false;
        }
    }
    public static async Task<NpgsqlConnection> GetAndOpenConnectionFactory()
    {
        var c = new NpgsqlConnection(Auth.Authentication.DatabaseConnectionString);
        await c.OpenAsync();
        return c;
    }
}


