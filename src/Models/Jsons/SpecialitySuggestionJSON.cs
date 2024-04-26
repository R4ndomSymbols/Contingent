namespace StudentTracking.Models.JSON;

[Serializable]
public class SpecialitySuggestionJSON {

    public int Id {get;}
    public string FullName {get; set;}

    public SpecialitySuggestionJSON(int id, string fgosName, string qualificationName, string fgosCode)
    {
        Id = id;
        FullName =
            fgosName + " | " + qualificationName + "(" + fgosCode + ")"; 

    }
}