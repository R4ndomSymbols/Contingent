using Contingent.Import;
using Contingent.Utilities;
using Contingent.Utilities;

namespace Contingent.Controllers.DTO.In;

public class OrderIdentityDTO : IFromCSV<OrderIdentityDTO>
{
    public const string OrderOrgIdFieldName = "приказ";
    public const string OrderSpecifiedYearFieldName = "дата приказа";

    public string OrgId { get; set; }
    public string OrderSpecifiedYear { get; set; }

    public OrderIdentityDTO()
    {
        OrgId = string.Empty;
        OrderSpecifiedYear = string.Empty;
    }

    public Result<OrderIdentityDTO> MapFromCSV(CSVRow row)
    {
        OrgId = row[OrderOrgIdFieldName]!;
        OrderSpecifiedYear = row[OrderSpecifiedYearFieldName]!;
        if (OrgId is null || OrderSpecifiedYear is null)
        {
            return Result<OrderIdentityDTO>.Failure(new ImportValidationError("Невозможно определить идентификатор приказа"));
        }
        return Result<OrderIdentityDTO>.Success(this);
    }
}
