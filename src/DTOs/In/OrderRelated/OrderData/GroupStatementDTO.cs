using Contingent.Import;
using Utilities;

namespace Contingent.Controllers.DTO.In;

public class GroupStatementDTO : IFromCSV<GroupStatementDTO>
{
    public int GroupId { get; set; }
    public string GroupName { get; set; }

    public GroupStatementDTO()
    {
        GroupId = Utils.INVALID_ID;
        GroupName = "";
    }

    public Result<GroupStatementDTO> MapFromCSV(CSVRow row)
    {
        GroupName = row[FlowImport.GroupFieldName]!;
        GroupName ??= "";
        return Result<GroupStatementDTO>.Success(this);
    }
}