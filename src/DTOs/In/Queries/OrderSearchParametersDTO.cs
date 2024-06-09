using System.Text.Json.Serialization;

namespace Contingent.Controllers.DTO.In;

public class OrderSearchParametersDTO
{
    public string SearchText { get; set; }
    public int? Year { get; set; }
    public int? Type { get; set; }
    public int PageSize { get; set; }

    public OrderSearchParametersDTO()
    {
        SearchText = "";
    }
}
