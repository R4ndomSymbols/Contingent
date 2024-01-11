namespace StudentTracking.Models.JSON;

[Serializable]
public class OrderModelJSON {

    public int Id {get; set; }
    public string SpecifiedDate {get; set; }
    public string EffectiveDate {get; set; }
    public string? EndDate {get; set; }
    public int OrderNumber {get; set; }
    public int OrderType {get; set; }
    public string OrderStringId {get; set; } 
    public string OrderDescription {get; set; }
    public string OrderDisplayedName {get; set; }

    public OrderModelJSON()
    {   
        SpecifiedDate = "";
        EffectiveDate = "";
        EndDate = null;
        OrderDescription = "";
        OrderDisplayedName = "";
        OrderStringId = "";
    }
}