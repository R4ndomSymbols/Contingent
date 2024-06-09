using Contingent.Import.CSV;
using Contingent.Utilities;

namespace Contingent.Import;

public class ImportTypeInfo
{
    public ImportType Type { get; private set; }
    public string RussianName { get; private set; }
    private ImportTypeInfo(ImportType type, string russianName)
    {
        Type = type;
        RussianName = russianName;
    }
    public static readonly IReadOnlyCollection<ImportTypeInfo> AvailableImports = new List<ImportTypeInfo>() {
        new(ImportType.Flow, "Движения"),
        new ImportTypeInfo(ImportType.Groups, "Группы (потоки)"),
        new ImportTypeInfo(ImportType.Orders, "Приказы"),
        new ImportTypeInfo(ImportType.Students, "Студенты"),
        new ImportTypeInfo(ImportType.Specialties, "Специальности")
    };

    public static ImportCSV? GetImporterByType(int type, Stream dataSource, ObservableTransaction scope)
    {
        var found = AvailableImports.FirstOrDefault(t => (int)t.Type == type);
        if (found is null)
        {
            return null;
        }
        return found.GetImport(dataSource, scope);
    }
    private ImportCSV? GetImport(Stream source, ObservableTransaction scope)
    {
        return Type switch
        {
            ImportType.Flow => new FlowImport(source, scope),
            ImportType.Groups => new GroupImport(source, scope),
            ImportType.Orders => new OrderImport(source, scope),
            ImportType.Students => new StudentImport(source, scope),
            ImportType.Specialties => new SpecialtyImport(source, scope),
            _ => null,
        };
    }

}



public enum ImportType
{
    Flow,
    Groups,
    Orders,
    Students,
    Specialties
}