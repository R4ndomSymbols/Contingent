using System.Reflection;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Utilities.Validation;

public static class ValidatorCollection {

    public static readonly Regex OnlyDigits = new Regex(@"^[0-9]+\z");
    public static readonly Regex Snils = new Regex(@"^[0-9]{3}-[0-9]{3}-[0-9]{3}[^\S\t\n\r][0-9]{2}\z");
    public static readonly Regex DecimalFormat = new Regex(@"^[0-9]+\.[0-9]+\z");
    public static readonly Regex OnlyRussianText = new Regex(@"^[A-я\s0-9]+$");
    public static readonly Regex OnlyRussianLetters = new Regex(@"^[\u0410-\u044f]+\z");
    public static readonly Regex RussianNamePart = new Regex(@"^[А-я]+([-][А-я]+)?\z");
    public static readonly Regex FgosCode = new Regex(@"^[0-9]{2}\.[0-9]{2}\.[0-9]{2}\z");

    // больше либо равно минимальному, меньше либо равно максимальному
    public static bool CheckRange(double? value, double min, double max){
         if (value == null){
           return false;
        }
        if (min > max){
            throw new ArgumentException("Минимальное должно быть меньше максимального");
        }
        else {
            return value >= min && value <= max;
        }
    }
    public static bool CheckRange(int? value, int min, int max){
         if (value == null){
           return false;
        }
        if (min > max){
            throw new ArgumentException("Минимальное должно быть меньше максимального");
        }
        else {
            return value >= min && value <= max;
        }
    }
    public static bool CheckRange(decimal? value, decimal min, decimal max){
         if (value == null){
           return false;
        }
        if (min > max){
            throw new ArgumentException("Минимальное должно быть меньше максимального");
        }
        else {
            return value >= min && value <= max;
        }
    }

    public static bool CheckStringLength(string? value, int lengthMin, int lengthMax){
        if (value == null){
            return false;
        }
        if (lengthMin > lengthMax || lengthMin < 0){
            throw new ArgumentException("Минимальное должно быть меньше максимального");
        }
        else{
            return value.Length <= lengthMax && value.Length >= lengthMin; 
        }
    }
    public static bool CheckStringPattern(string? value, Regex expression){
        if (value == null){
            return false;
        }
        return expression.Match(value).Captures.Count > 0;
    }
    public static bool CheckStringPatterns(string? value, IEnumerable<Regex> expressions){
        if (value == null){
            return false;
        }
        return expressions.Any(x => x.Match(value).Captures.Count > 0);
    }



}


