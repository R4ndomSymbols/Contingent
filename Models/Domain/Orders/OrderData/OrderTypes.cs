namespace StudentTracking.Models.Domain.Orders;


public enum OrderTypes
{
    // пустой приказ
    EmptyOrder = 0,

    //
    //    Список всех видов приказов
    //    **Зачисление**:
    //    Переводом
    FreeEnrollmentWithTransfer = 17,
    //    В порядке восстановления
    FreeReenrollment = 18,
    //    На первый курс
    FreeEnrollment = 11,

    //    **Перевод внутри организации**
    //    Перевод на следующий курс
    FreeTransferNextCourse = 13,
    //    Перевод на другую специальность
    FreeTransferBetweenSpecialities = 14,
    //    Перевод с платного на бесплатное (только ДК)

    //        **Отчисление**
    //    В связи с переводом в другую организацию
    //    В связи с невыходом из академического отпуска
    //    В связи в неприступлением к обучению
    //    В связи с неуспеваемостью
    FreeDeductionWithAcademicDebt = 16,
    //    В связи с выпуском
    FreeDeductionWithGraduation = 12,
    //    По собственному желанию
    FreeDeductionWithOwnDesire = 15,





    //         **Остальное**
    //    О предоставлении академического отпуска
    //    О смене фамилии
    //    О восстановлении из академического отпуска


    // дополнительный контингент
    // зачисление внебюджет
    // у студента обязательно договор
    // группа внебюджет
    // специальность соответствует уровню образования студента



    //
    //    Список всех видов приказов
    // ------------**Зачисление**:

    // Переводом
    //      студент:
    //      отчислен или не светился в истории
    //      группа:
    //      любой курс
    PaidEnrollmentWithTransfer = 30,

    // В порядке восстановления
    //      студент:
    //      отчислен или не светился в истории
    //      группа:
    //      любой курс 
    PaidReenrollment = 31,

    // На первый курс
    //      студент:
    //      никогда не зачислялся или отчислен в связи с выпуском
    //      или отчислен по собтвенному желанию или в связи с неуспеваемостью, но прошло 5 и более лет
    //      группа:
    //      первый курс
    //      или второй, но тогда у студента должно быть среднее полное образование
    PaidEnrollment = 32,

    //
    // ------------------**Перевод внутри организации**
    // Перевод на следующий курс
    //      студент:
    //      зачислен или переведен в группу, не имеющую статуса выпускной
    //      группа:  
    //      имеет тот же id последовательности, что и текущая группа студента
    //      разница в курсе 1, прыжки недопустимы
    PaidTransferNextCourse = 33,

    // Перевод на другую специальность
    //      студент:
    //      любой зачисленный
    //      группа: 
    //      имеет ту же специальность, что текущая группа и тот же курс, 
    PaidTransferBetweenSpecialities = 34,
    // Перевод с платного на бесплатное (только ДК)
    //      студент: 
    //      зачислен
    //      группа:
    //      тот же курс, та же специальность, но бесплатная, совпадение года создания
    PaidTransferFromPaidToFree = 35,


    // 
    // ------------------ **Отчисление**
    // В связи с переводом в другую организацию
    //      студет: 
    //      зачислен
    //      группа:
    //      не указывается
    PaidDeductionWithTransfer = 36,
    // В связи с невыходом из академического отпуска
    //      студент: 
    //      находится в академическом отпуске
    //      группа:
    //      не указывается
    PaidDeductionWithAcademicVacationNoReturn = 37,
    // В связи в неприступлением к обучению
    //      студент: 
    //      зачислен (какой-то интервал времени после зачисления)
    //      группа:
    //      не указывается
    PaidDeductionWithEducationProcessNotIniciated = 38,
    // В связи с неуспеваемостью
    //      студент:
    //      любой
    //      группа:
    //      не указывается  
    PaidDeductionWithAcademicDebt = 39,

    // В связи с выпуском
    //      студент: 
    //      любой, зачисленый или переведенный в ниженазванную группу
    //      группа:
    //      текущая группа студента должна иметь выпускной курс
    PaidDeductionWithGraduation = 40,

    // По собственному желанию
    //      студент:
    //      любой зачисленный
    //      группа: 
    //      не указывается
    PaidDeductionWithOwnDesire = 41,
    // 
    // **Остальное**
    // О предоставлении академического отпуска
    //      студент:
    //      любой зачисленный
    //      группа: 
    //      не указывается (как бы остается прежней, но в данном случае полностью утрачивается)
    PaidAcademicVacationSend = 42,
    // О смене фамилии
    // О восстановлении из академического отпуска
    //      студент:
    //      студент должен находится в академическом отпуске
    //      группа: 
    //      должна иметь ту же специальность, тот же курс
    PaidAcademicVacationReturn = 43,
    //
    //


   

    
    // перевод внебюджет на следующий курс
    
    // перевод внебюджет между специальностями
    
    // отчисление по собственному желанию
   
    // отчисление в связи с академической задолженностью
    

    // зачисление в связи с переводом
    

    // зачисление в порядке восстановления
    

}