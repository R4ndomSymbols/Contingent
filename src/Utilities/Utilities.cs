using System.Globalization;
using Npgsql;
using Contingent.Models;
using System.Security.Cryptography.X509Certificates;

namespace Utilities;


public static class Utils
{

    private static string? DatabaseConnectionString = null;
    public const int INVALID_ID = -1;

    public static bool IsValidId(int id){
        return id != INVALID_ID; 
    }
    public static bool IsValidId(int? id){
        return id != null && IsValidId(id.Value);
    }


    public static string FormatDateTime(DateTime? date, bool expand = false)
    {
        var correctDate = date == null ? DateTime.Now : (DateTime)date;
        if (expand)
        {
            return string.Format("{0} {1} {2}",
            correctDate.Day,
            correctDate.ToString("MMMM", CultureInfo.CreateSpecificCulture("ru")),
            correctDate.Year.ToString() + " года");
        }
        return string.Format("{0}.{1}.{2}", correctDate.Day, correctDate.Month, correctDate.Year);
    }
    public static DateTime ParseDate(string? date)
    {
        if (date == null)
        {
            throw new ArgumentNullException();
        }
        else
        {
            var parts = date.Split('.');
            if (parts.Length != 3)
            {
                throw new ArgumentException("Неверный формат даты");
            }
            int day = Convert.ToInt32(parts[0]);
            int month = Convert.ToInt32(parts[1]);
            int year = Convert.ToInt32(parts[2]);
            return new DateTime(year, month, day);
        }
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
        if (DatabaseConnectionString == null)
        {
            DatabaseConnectionString = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build()["ConnectionString"];
        }
        var c = new NpgsqlConnection(DatabaseConnectionString);
        await c.OpenAsync();
        return c;
    }
}


