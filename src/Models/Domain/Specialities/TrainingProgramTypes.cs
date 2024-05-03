namespace Contingent.Models.Domain.Specialities;


public class TrainingProgram
{
    private string[] _aliases;
    public string Name { get; private set; }
    public TrainingProgramTypes Type { get; private set; }
    public static TrainingProgram None = new TrainingProgram
    (
        "Не указано",
        TrainingProgramTypes.NotMentioned,
        new string[] { "не указано", "", "нет" }
    );

    private TrainingProgram(string name, TrainingProgramTypes type, string[] aliases)
    {
        Name = name;
        Type = type;
        _aliases = aliases;
    }
    public static IReadOnlyCollection<TrainingProgram> Types = new List<TrainingProgram>() {
        new TrainingProgram(
            "Не указано",
            TrainingProgramTypes.NotMentioned,
            new string[] {"не указано", "", "нет"}
        ),
        new TrainingProgram
        (
            "Программа подготовки квалифицированных рабочих, служащих",
            TrainingProgramTypes.QualifiedWorker,
            new string[] {"квалифицированные рабочие", "рабочие"}
        ),
        new TrainingProgram(
            "Программа подготовки специалистов среднего звена",
             TrainingProgramTypes.GenericSpecialist,
            new string[] {"специалисты среднего звена", "специалисты"}
        )

    }.AsReadOnly();

    public static bool TryGetByType(int type, out TrainingProgram? result)
    {
        result = Types.FirstOrDefault(t => (int)t.Type == type, null);
        return result is not null;
    }
    public static TrainingProgram GetByType(int type)
    {
        var result = TryGetByType(type, out TrainingProgram? toReturn);
        if (result)
        {
            return toReturn!;
        }
        else
        {
            throw new Exception("Тип подготовки не определен");
        }
    }
    public static int ImportProgramTypeCode(string? programName)
    {
        if (programName is null)
        {
            return (int)TrainingProgramTypes.NotMentioned;
        }
        var lower = programName.ToLower();
        var found = Types.FirstOrDefault(t => t.Name.ToLower() == lower || t._aliases.Any(x => x.ToLower() == lower), null);
        if (found is null)
        {
            return (int)TrainingProgramTypes.NotMentioned;
        }
        return (int)found.Type;
    }

    public bool IsDefined()
    {
        return Type != TrainingProgramTypes.NotMentioned;
    }

}


public enum TrainingProgramTypes
{
    NotMentioned = 0,
    QualifiedWorker = 1,
    GenericSpecialist = 2,
}
