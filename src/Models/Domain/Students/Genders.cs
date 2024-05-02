namespace StudentTracking.Models.Domain.Students;

public static class Genders
{

    public enum GenderCodes
    {
        Undefined = 0,
        Male = 1,
        Female = 2
    }

    public static readonly Dictionary<GenderCodes, string> Names = new Dictionary<GenderCodes, string>{
        {GenderCodes.Undefined, "Не указано"},
        {GenderCodes.Male, "Мужчина"},
        {GenderCodes.Female, "Женщина"},
    };
    private static readonly Dictionary<string, GenderCodes> ImportDictionary = new Dictionary<string, GenderCodes>{
        {"мужчина", GenderCodes.Male},
        {"женщина", GenderCodes.Female},
        {"ж", GenderCodes.Female},
        {"м", GenderCodes.Male},
    };

    public static GenderCodes ImportGender(string? gender)
    {
        if (gender is null)
        {
            return GenderCodes.Undefined;
        }
        if (ImportDictionary.TryGetValue(gender.ToLower(), out var result))
        {
            return result;
        }
        return GenderCodes.Undefined;
    }
}

