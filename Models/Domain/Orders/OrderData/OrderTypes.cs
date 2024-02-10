namespace StudentTracking.Models.Domain.Orders;


public enum OrderTypes
{
    NoOrder = 0,
    // зачисление
    Enrollment = 1,
    // отчисление по собственному желанию
    DeductionWithOwnDesire = 2,
    // отчисление из-за неуспеваемости
    DeductionWithPoorProgress = 3,
    // отчисление в связи с выпуском 
    DeductionWithGraduation = 4,
    // перевод из группы в группу
    TransferGroupToGroup = 5,
    // перевод из другой организации в группу
    TransferOrgToGroup = 6,
    // академический отпуск
    AcademicVacationSend = 7,
    // восстановление из отпуска
    AcademicVacationReturn = 8,
    // восстановление после отчисления
    ReenrollmentAfterDeduction = 9,
    // перевод с платного на бесплатное
    FromPaidToFreeGroup = 10,

    FreeEnrollment = 11,
    FreeDeductionWithGraduation = 12,
    FreeNextCourseTransfer = 13,

}