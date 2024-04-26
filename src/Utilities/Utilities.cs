using System.Globalization;
using Npgsql;
using StudentTracking.Models;

namespace Utilities;


public static class Utils {

    private static string? DatabaseConnectionString = null;


    public const int INVALID_ID = 0;
    public const int ORG_CREATION_YEAR = 1972; 

    public static string FormatDateTime(DateTime? date, bool expand = false){
        var correctDate = date == null ? DateTime.Now : (DateTime)date;
        if (expand){
            return string.Format("{0} {1} {2}", 
            correctDate.Day, 
            correctDate.ToString("MMMM", CultureInfo.CreateSpecificCulture("ru")), 
            correctDate.Year.ToString() + " года");
        }
        return string.Format("{0}.{1}.{2}", correctDate.Day, correctDate.Month, correctDate.Year);
    }
    public static DateTime ParseDate(string? date){
        if (date == null){
            throw new ArgumentNullException();
        }
        else{
            var parts = date.Split('.');
            if (parts.Length != 3){
                throw new ArgumentException("Неверный формат даты");
            }
            int day = Convert.ToInt32(parts[0]);
            int month = Convert.ToInt32(parts[1]);
            int year = Convert.ToInt32(parts[2]);
            return new DateTime(year, month, day);
        }
    }
    public static bool TryParseDate(string? date){
        if (string.IsNullOrEmpty(date)){
            return false;
        }
        else{
            var parts = date.Split('.');
            if (parts.Length != 3){
                return false;
            }
            if(int.TryParse(parts[0], out int day)){
                if(int.TryParse(parts[1], out int month)){
                    if(int.TryParse(parts[2], out int year)){
                        try {
                            new DateTime(year, month, day);
                            return true;
                        }
                        catch (ArgumentOutOfRangeException){
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

    public static string FormatToponymName(string name){

        if (string.IsNullOrEmpty(name)){
            return "";
        }
        string[] split = name.Split(new char[] {' ', '-'});
        if (split.Any(x => x == string.Empty)){
            throw new ArgumentException("Входной топоним не был в правильном формате");
        }
        int point = 0;
        string result = string.Empty;
        for (int i = 0; i < split.Length; i++){
            var newName = string.Concat(split[i][0..1].ToUpper(), split[i].Substring(1).ToLower());
            point+=split[i].Length;
            result+=newName + (point < name.Length ? name[point] : "");
            point++;
        }
        return result; 
    }

    public static async Task<NpgsqlConnection> GetAndOpenConnectionFactory(){
        if (DatabaseConnectionString == null){
            DatabaseConnectionString = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build()["ConnectionString"];
        }
        var c = new NpgsqlConnection(DatabaseConnectionString);
        await c.OpenAsync(); 
        return c;
    }
    // метод не экранирует " внутри строки
    public static List<(int start, int length)> SplitJsonIntoObjectStrings(string json, int level){
        Stack<int> positionHolder = new Stack<int>();
        int currentLevel = 0;
        bool escaped = false;
        List<(int start, int length, int level)> foundObjects = new List<(int start, int length, int level)>();
        for (int i = 0; i < json.Length; i++)
        {
            if (json[i] == '{' && !escaped){
                positionHolder.Push(i);
                currentLevel++;
            }
            else if (json[i] == '}' && !escaped){
                //Console.WriteLine(currentLevel + " close");
                var start = positionHolder.Pop();
                foundObjects.Add((start, i - start + 1, currentLevel));
                currentLevel--;
            }
            else if (json[i] == '\"') {
                escaped = !escaped;
            } 
        }
        //Console.WriteLine(string.Join("\n", foundObjects.Select(x => x.start + " " + x.length + " " + x.level)));
        if (positionHolder.Any()){
            throw new ArgumentException("Неверный формат json");
        }
        return foundObjects.Where(x => x.level >= level).Select(x => (x.start, x.length)).ToList();
    }

    public static int ComputeGroupCourse(int attendationYear, int eduProgramCourseCount){
        int difference = DateTime.Now.Year - attendationYear;
        if (difference < 0){
            throw new ArgumentException("Группа не может быть создана в следующем году");
        }
        if (eduProgramCourseCount < SpecialityModel.MINIMAL_COURSE_COUNT || 
            eduProgramCourseCount > SpecialityModel.MAXIMUM_COURSE_COUNT){
            throw new ArgumentException("Неверное значение количества курсов у специальности");         
        }
        int resultCourseCount = difference+1;
        if (resultCourseCount > eduProgramCourseCount){
            return eduProgramCourseCount;
        }
        else {
            return resultCourseCount;
        }
    }

}


