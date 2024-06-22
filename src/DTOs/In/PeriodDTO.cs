using System.Text.Json.Serialization;

namespace Contingent.DTOs.In;

public class PeriodDTO
{
    [JsonRequired]
    public string StartDate { get; set; }
    public string? EndDate { get; set; }

    public PeriodDTO()
    {
        StartDate = "";
        EndDate = null;
    }

}

