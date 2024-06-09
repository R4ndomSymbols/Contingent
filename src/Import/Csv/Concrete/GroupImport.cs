using Contingent.Controllers.DTO.In;
using Contingent.Models.Domain.Groups;
using Contingent.Utilities;

namespace Contingent.Import.CSV;

public class GroupImport : ImportCSV
{
    private List<GroupModel> _groups = new List<GroupModel>();

    public GroupImport(Stream dataSource, ObservableTransaction scope) : base(dataSource, scope)
    {
        _groups = new List<GroupModel>();
    }

    public override ResultWithoutValue Save(bool commit)
    {
        foreach (var group in _groups)
        {
            group.Save(_scope);
        }
        FinishImport(commit);
        return ResultWithoutValue.Success();
    }

    public override ResultWithoutValue Import()
    {
        if (_groups.Any())
        {
            return ResultWithoutValue.Success();
        }

        var dtos = Read(() => new GroupInDTO(), out List<CSVRow> rows);
        if (dtos.IsFailure)
        {
            return ResultWithoutValue.Failure(dtos.Errors);
        }
        foreach (var groupDTO in dtos.ResultObject)
        {
            var group = GroupModel.Build(groupDTO, _scope);
            if (group.IsFailure)
            {
                return ResultWithoutValue.Failure(group.Errors);
            }
            _groups.Add(group.ResultObject);
            // проблема в том, что группа получает неверные
            // идентификаторы потока и т.д.
            // потому что база данных не может отследить несохраненное состояние
            group.ResultObject.Save(_scope);
        }
        return ResultWithoutValue.Success();
    }
}