using StudentTracking.Controllers.DTO.In;
using StudentTracking.Models.Domain.Flow;
using StudentTracking.Models.Domain.Flow.History;
using StudentTracking.Models.Domain.Orders;
using StudentTracking.Statistics;

namespace StudentTracking.Models.Infrastruture;


public class SearchHelper{

    public SearchHelper(){

    }

    public Filter<StudentFlowRecord> GetFilter(StudentSearchQueryDTO query){
        var filter = Filter<StudentFlowRecord>.Empty;
        if (!string.IsNullOrEmpty(query.Name))
        {
            var normalized = query.Name.Trim();
            if (normalized.Length >= 3)
            {
                filter.Include(
                new Filter<StudentFlowRecord>(
                    (source) => source.Where(
                        rec =>
                        {
                            var found = rec?.Student;
                            if (found is null){
                                return false;
                            }
                            else {
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
                filter.Include(
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
    public Func<IEnumerable<StudentFlowRecord>>? GetSource(StudentSearchQuerySourceDTO dto){
        if (dto is null){
            return null;
        }
        if (dto.OrderId != null && dto.OrderMode != null){
            if (dto.OrderMode == FlowHistory.OrderRelationMode.OnlyExcluded.ToString()){
                return () => {
                    var order = Order.GetOrderById(dto.OrderId.Value).Result.ResultObject;
                    return FlowHistory.GetRecordsByFilter(new SQL.QueryLimits(0,500),
                    new HistoryExtractSettings{
                        ExtractByOrder = (order, FlowHistory.OrderRelationMode.OnlyExcluded),
                        ExtractLastState = true,
                        ExtractGroups = true,
                        ExtractStudents = true,
                        ExtractOrders = false
                    });
                };
            }
            else if (dto.OrderMode == FlowHistory.OrderRelationMode.OnlyIncluded.ToString()){
                return () => {
                    var order = Order.GetOrderById(dto.OrderId.Value).Result.ResultObject;
                    return FlowHistory.GetRecordsByFilter(new SQL.QueryLimits(0,500),
                    new HistoryExtractSettings{
                        ExtractByOrder = (order, FlowHistory.OrderRelationMode.OnlyIncluded),
                        ExtractLastState = false,
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

    public Filter<SpecialityModel> GetFilterForSpecialities(SpecialitySearchQueryDTO dto){
        var filter = Filter<SpecialityModel>.Empty;
        if (dto is null){
            return filter;
        }
        if (dto.SearchString is not null && dto.SearchString.Length >= 3){
            filter = filter.Include(
                new Filter<SpecialityModel>(
                    (spec) => spec.Where(
                        s => s.FgosCode.Contains(dto.SearchString, StringComparison.OrdinalIgnoreCase) 
                        || s.FgosName.Contains(dto.SearchString,StringComparison.OrdinalIgnoreCase)
                        || s.Qualification.Contains(dto.SearchString, StringComparison.OrdinalIgnoreCase)
                    )
                )
            );
        }
        return filter;
        
    }        


}