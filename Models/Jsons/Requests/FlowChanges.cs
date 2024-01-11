using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace StudentTracking.Models.JSON;


[Serializable]
public class OrderInStudentFlowJSON {

    [JsonRequired]
    public int OrderId {get; set;}
    [JsonRequired]
    public List<StudentInOrderRecordJSON> Records {get; set;}

    public OrderInStudentFlowJSON() {
        Records = new List<StudentInOrderRecordJSON>();
    }

}
[Serializable]
public struct StudentInOrderRecordJSON {

    [JsonRequired]
    public int StudentId;
    [JsonRequired]
    public int GroupToId;

}



