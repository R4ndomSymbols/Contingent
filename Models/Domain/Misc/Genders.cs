namespace StudentTracking.Models.Domain.Misc;

public static class Genders {

    public enum GenderCodes {
        Undefinned = 0,
        Male = 1,
        Female = 2 
    }  

    public static readonly Dictionary<GenderCodes, string> Names = new Dictionary<GenderCodes, string>{
        {GenderCodes.Undefinned, "Не указано"},
        {GenderCodes.Male, "Мужчина"},
        {GenderCodes.Female, "Женщина"},
    };
}

