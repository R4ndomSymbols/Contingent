namespace StudentTracking.Models.JSON;


public class AddressSuggestionJSON {

    public string SuggestionText {get; set;} = "";
    public int? FederalCode {get; set;} = null;
    public int? DistrictID {get; set;} = null;
    public int? SettlementAreaID {get; set;} = null;
    public int? SettlementID {get; set;}= null;
    public int? StreetID {get; set;}= null;
    public int? BuildingID {get; set;}= null;
}