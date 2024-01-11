namespace StudentTracking.Models.Domain.Orders.Fillers;

// это сущность, проводящая студентов по приказам
// проведение осуществляется один раз, после чего использовать
// сущность повторно нельзя
public abstract class Filler {

    protected int _orderId;
    public abstract Task<bool> ConductByOrder(); 
}




