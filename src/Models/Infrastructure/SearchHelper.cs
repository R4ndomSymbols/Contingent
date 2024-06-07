using Contingent.Controllers.DTO.In;
using Contingent.Models.Domain.Flow;
using Contingent.Models.Domain.Orders;
using Contingent.Models.Domain.Groups;
using Contingent.Models.Domain.Specialties;
using Contingent.Statistics;

namespace Contingent.Models.Infrastructure;


public class SearchHelper
{

    public SearchHelper()
    {

    }

    public Filter<StudentFlowRecord> GetFilter(StudentSearchQueryDTO query)
    {
        var filter = Filter<StudentFlowRecord>.Empty;
        if (!string.IsNullOrEmpty(query.Name))
        {
            var normalized = query.Name.Trim();
            if (normalized.Length >= 3)
            {
                filter = filter.Include(
                new Filter<StudentFlowRecord>(
                    (source) => source.Where(
                        rec =>
                        {
                            var found = rec?.Student;
                            if (found is null)
                            {
                                return false;
                            }
                            else
                            {
                                return found.GetName().Contains(normalized, StringComparison.OrdinalIgnoreCase);
                            }

                        }
                    )
                ));
            }
        }
        if (!string.IsNullOrEmpty(query.GroupName))
        {
            var normalized = query.GroupName.Trim();
            if (normalized.Length >= 2)
            {
                filter = filter.Include(
                new Filter<StudentFlowRecord>(
                    (source) => source.Where(
                        rec => rec.GroupTo is null ? GroupModel.InvalidNamePlaceholder.ToLower() == normalized.ToLower() :
                        rec.GroupTo.GroupName.Contains(normalized, StringComparison.OrdinalIgnoreCase)
                    )
                ));
            }
        }
        return filter;
    }
    // получает фильтрованный источник для дальнейшей фильтрации
    // существует по причинам производительности
    public Func<IEnumerable<StudentFlowRecord>>? GetSource(StudentSearchQuerySourceDTO dto)
    {
        if (dto is null)
        {
            return null;
        }
        if (dto.OrderId != null && dto.OrderMode != null)
        {
            var order = Order.GetOrderById(dto.OrderId.Value);
            if (order is null)
            {
                return null;
            }
            if (dto.OrderMode == OrderRelationMode.OnlyExcluded.ToString())
            {
                return () =>
                {
                    return FlowHistory.GetRecordsByFilter(new SQL.QueryLimits(0, 500),
                    new HistoryExtractSettings
                    {
                        ExtractByOrder = (order, OrderRelationMode.OnlyExcluded),
                        ExtractAbsoluteLastState = true,
                        ExtractGroups = true,
                        ExtractStudents = true,
                        ExtractOrders = false
                    });
                };
            }
            else if (dto.OrderMode == OrderRelationMode.OnlyIncluded.ToString())
            {
                return () =>
                {
                    return FlowHistory.GetRecordsByFilter(new SQL.QueryLimits(0, 500),
                    new HistoryExtractSettings
                    {
                        ExtractByOrder = (order, OrderRelationMode.OnlyIncluded),
                        ExtractAbsoluteLastState = false,
                        ExtractOrders = false,
                        ExtractStudentUnique = true,
                        ExtractGroups = true,
                        ExtractStudents = true
                    });
                };
            };
        }
        return null;
    }

    public Filter<SpecialtyModel> GetFilterForSpecialties(SpecialtySearchQueryDTO dto)
    {
        var filter = Filter<SpecialtyModel>.Empty;
        if (dto is null)
        {
            return filter;
        }
        if (dto.SearchString is not null && dto.SearchString.Length >= 3)
        {
            filter = filter.Include(
                new Filter<SpecialtyModel>(
                    (spec) => spec.Where(
                        s => s.FgosCode.Contains(dto.SearchString, StringComparison.OrdinalIgnoreCase)
                        || s.FgosName.Contains(dto.SearchString, StringComparison.OrdinalIgnoreCase)
                        || s.Qualification.Contains(dto.SearchString, StringComparison.OrdinalIgnoreCase)
                    )
                )
            );
        }
        return filter;

    }

    public Filter<Order> GetFilterForOrder(OrderSearchParametersDTO? dto)
    {
        var filter = Filter<Order>.Empty;
        if (dto is null)
        {
            return filter;
        }
        if (!string.IsNullOrEmpty(dto.SearchText))
        {
            var normalized = dto.SearchText.Trim().ToLower();
            if (normalized.Length >= 3)
            {
                filter = filter.Include(
                    new Filter<Order>(
                        (source) => source.Where(o => o.OrderDisplayedName.Contains(normalized, StringComparison.OrdinalIgnoreCase))
                    )
                );
            }
        }
        if (dto.Year is not null)
        {
            filter = filter.Include(
                new Filter<Order>(
                    (source) => source.Where(o => o.SpecifiedDate.Year == dto.Year)
                )
            );
        }
        if (dto.Type is not null)
        {
            filter = filter.Include(
                new Filter<Order>(
                    (source) => source.Where(o => (int)o.GetOrderTypeDetails().Type == dto.Type)
                )
            );
        }
        return filter;
    }



}