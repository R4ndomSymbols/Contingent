namespace StudentTracking.Models.Domain.Flow;
// статусы студента, связанные с отношением к колледжу
public enum StudentStates {
    CycleRecorded = 0,
    Enlisted = 1,
    Deducted = 2,   
    NotRecorded = 3
}