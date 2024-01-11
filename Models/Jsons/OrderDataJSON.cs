using StudentTracking.Models.Domain.Orders.OrderData;

namespace StudentTracking.Models.JSON;

public class OrderDataJSON {
    public IEnumerable<StudentMove> Moves;
    public IEnumerable<StudentStatement> Ends;

}
