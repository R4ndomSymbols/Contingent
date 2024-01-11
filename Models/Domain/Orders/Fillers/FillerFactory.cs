namespace StudentTracking.Models.Domain.Orders.Fillers;

// это фабрика филлеров
public abstract class Filler {

    protected int _orderId;
    public abstract Task<bool> ConductByOrder(); 
}


