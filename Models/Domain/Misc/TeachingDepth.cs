namespace StudentTracking.Models.Domain.Misc;

public class TeachingDepth {

    public enum Levels {
        NotMentioned = 0,
        Common = 1,
        Advanced = 2
    }

    public static readonly Dictionary<Levels, string> LevelNames = new Dictionary<Levels, string>{
        {Levels.NotMentioned, "Не указано"},
        {Levels.Common,"Базовый"},
        {Levels.Advanced,"Углубленный"}

    };

}