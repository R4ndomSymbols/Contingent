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
}