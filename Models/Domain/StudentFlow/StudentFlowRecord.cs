using StudentTracking.Models.Domain.Orders;

namespace StudentTracking.Models.Domain.Flow;

// представляет собой запись в таблице движения студентов 

public struct StudentFlowRecord {

    private int _id;
    private int _studentId;
    private int? _groupToId;
    private int _orderId;
    private DateTime _orderEffectiveDate;
    private OrderTypes _orderType; 

    public int Id { 
        get => _id; 
    }
    public int? GroupToId 
    { 
        get => _groupToId; 
    }
    public int OrderId 
    { 
        get => _orderId; 
    }
    public DateTime OrderEffectiveDate{
        get => _orderEffectiveDate;
    }
    public OrderTypes OrderType {
        get => _orderType;
    }
    public int StudentId {
        get => _studentId;
    }

    public StudentFlowRecord(int id,
        int orderId,
        int studentId,
        int? groupToId,
        DateTime orderEffectiveDate,
        OrderTypes type){
        _id = id;
        _orderId = orderId;
        _groupToId = groupToId;
        _orderEffectiveDate = orderEffectiveDate;
        _orderType = type; 
        _studentId = studentId;
    }
    public StudentFlowRecord(
        int orderId,
        int studentId,
        int? groupToId){
        _id = 0;
        _orderId = orderId;
        _groupToId = groupToId;
        _studentId = studentId;
    }  


} 