namespace StudentTracking.Models.Domain.Orders;


public enum OrderTypes
{   
    // пустой приказ
    EmptyOrder = 0,

    /*
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
    */


    // зачисление на бюджет
    FreeEnrollment = 11,
    // отчисление с бюджета
    FreeDeductionWithGraduation = 12,
    // перевод на следующий курс
    FreeNextCourseTransfer = 13,
    // перевод между специальностями
    FreeTransferBetweenSpecialities = 14,
    // отчисление по собственному желанию
    FreeDeductionWithOwnDesire = 15, 
    // отчисление в связи с академической задолженностью
    FreeDeductionWithAcademicDebt = 16,
    // зачисление в связи с переводом
    FreeEnrollmentWithTransfer = 17,
    // зачисление в порядке восстановления 
    FreeReenrollment = 18


}