using System.Text.Json.Serialization;

namespace Contingent.DTOs.In;

public class PeriodDTO
{
    public string? StartDate { get; set; }
    [JsonRequired]
    public string EndDate { get; set; }

    public PeriodDTO()
    {
        StartDate = null;
        EndDate = "";
    }

}

