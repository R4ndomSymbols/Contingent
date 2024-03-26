namespace StudentTracking.Models.Domain.Orders.Infrastructure;

public class PaidOrderSequentialGuardian {

    private List<(AdditionalContingentOrder order, int bias)> _foundPaid;

    private PaidOrderSequentialGuardian(){
        
    }

}