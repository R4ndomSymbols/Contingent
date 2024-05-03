namespace Contingent.Models.Domain.Orders.OrderData;

public class OrderSearchParameters
{

    private int? _year;
    public DateTime? StartDate;
    public DateTime? EndDate;
    public string? OrderOrgId;
    public int? Year
    {
        get { return _year; }
        set
        {
            if (value is not null)
            {
                StartDate = null;
                EndDate = null;
            }
            _year = value;

        }
    }

    public OrderSearchParameters()
    {

    }

}
