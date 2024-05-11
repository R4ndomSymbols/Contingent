using Contingent.Controllers.DTO.In;
using Contingent.Models.Domain.Groups;
using Utilities;

namespace Contingent.Import;

public class GroupImport : IFromCSV<GroupImport>
{
    public GroupModel? Group { get; set; }

    public GroupImport()
    {
        Group = null;
    }
    public Result<GroupImport> MapFromCSV(CSVRow row)
    {
        var groupDTO = new GroupInDTO().MapFromCSV(row).ResultObject;
        var group = GroupModel.Build(groupDTO);
        if (group.IsFailure)
        {
            return Result<GroupImport>.Failure(group.Errors);
        }
        Group = group.ResultObject;
        return Result<GroupImport>.Success(this);
    }
}