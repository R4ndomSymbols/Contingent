namespace Contingent.Import;

public class ImportTypeInfo
{

    public ImportType Type { get; set; }
    public string RussianName { get; set; }

    private ImportTypeInfo(ImportType type, string russianName)
    {
        Type = type;
        RussianName = russianName;
    }
    public static IReadOnlyCollection<ImportTypeInfo> AvailableImports = new List<ImportTypeInfo>() {
        new ImportTypeInfo(ImportType.Flow, "Движения"),
        new ImportTypeInfo(ImportType.Groups, "Группы (потоки)"),
        new ImportTypeInfo(ImportType.Orders, "Приказы"),
        new ImportTypeInfo(ImportType.Students, "Студенты"),
        new ImportTypeInfo(ImportType.Specialties, "Специальности")
    };
}



public enum ImportType
{
    Flow,
    Groups,
    Orders,
    Students,
    Specialties
}