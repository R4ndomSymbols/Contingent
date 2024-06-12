namespace Contingent.DTOs.Out;

public class RedirectOptions
{

    public string DisplayURL { get; set; }
    public string RequestType { get; set; }

    public RedirectOptions()
    {
        DisplayURL = "/";
        RequestType = "GET";
    }

}
