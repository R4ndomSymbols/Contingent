namespace Contingent.Models.Domain.Flow;
// статусы студента, связанные с отношением к колледжу
public enum StudentStates
{
    NotRecorded = 0,
    Enlisted = 1,
    Deducted = 2,
    EnlistedInAcademicVacation = 3
}