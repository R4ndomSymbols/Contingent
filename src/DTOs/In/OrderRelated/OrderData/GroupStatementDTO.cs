using Contingent.Import;
using Contingent.Utilities;

namespace Contingent.Controllers.DTO.In;

public class GroupStatementDTO : IFromCSV<GroupStatementDTO>
{
    public const string GroupNameFieldName = "Название группы";
    public int GroupId { get; set; }
    public string GroupName { get; set; }

    public GroupStatementDTO()
    {
        GroupId = Utils.INVALID_ID;
        GroupName = "";
    }

    public Result<GroupStatementDTO> MapFromCSV(CSVRow row)
    {
        GroupName = row[GroupNameFieldName]!;
        GroupName ??= "";
        return Result<GroupStatementDTO>.Success(this);
    }
}