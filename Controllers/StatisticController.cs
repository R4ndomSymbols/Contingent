using Microsoft.AspNetCore.Mvc;
using StudentTracking.Statistics;
using StudentTracking.SQL;
using StudentTracking.Models;
using StudentTracking.Models.Domain.Misc;
using NpgsqlTypes;
using StudentTracking.Models.Domain.Orders.OrderData;
using StudentTracking.Models.Domain;
using StudentTracking.Models.Domain.Flow;

namespace StudentTracking.Controllers;

public class StatisticController : Controller {

    private readonly ILogger<HomeController> _logger;
    public StatisticController(ILogger<HomeController> logger)
    {
        _logger = logger;
    }
    // добавить уровень квалификации (специалист или квалифицированный рабочий)


    [HttpGet]
    [Route("statistics/age")]
    public IActionResult GetAgeStatistics(){
              
        var verticalRoot = new ColumnHeaderCell<StudentModel>();
        /*
        var specialitiesFilter = new ConstrainedColumnHeaderCell("Программы подготовки специалистов среднего звена",
            new ComplexWhereCondition(
            new WhereCondition(
            new Column("specialities", "specialities"),  WhereCondition.Relations.Equal,))
        */
        // 1 уровень
        var parameterNameCell1 = new ColumnHeaderCell<StudentModel>(
            "Наименование показателей",
            verticalRoot
        );
        var rowNumberCell = new ColumnHeaderCell<StudentModel>(
            "№ строки",
            verticalRoot
        );
        
        var trTypeFilter1 = new Filter<StudentModel>(
            (students) => 
                students.Where(
                    std =>{
                        var result = std.CurrentSpeciality;
                        if (result is null){
                            return false;
                        } 
                        return result.ProgramType.Type == TrainingProgramTypes.QualifiedWorker;
                    } 
                )
        );
        var trTypeCell1 = new ColumnHeaderCell<StudentModel>(
            "Программы подготовки квалифицированных рабочих, служащих",
            verticalRoot,
            trTypeFilter1);
        // 2 уровень
        var baseEduFilter2 = new Filter<StudentModel>(
            (students) => students.Where((std) =>
            {
                var result = std.CurrentSpeciality;
                if (result is null){
                    return false;
                } 
                return result.EducationalLevelIn.LevelCode == LevelsOfEducation.BasicGeneralEducation;
            })
        );
        var baseEduCell2 = new ColumnHeaderCell<StudentModel>(
            "На базе основного общего образования",
            trTypeCell1,
            baseEduFilter2
        );
        var midEduFilter2 = new Filter<StudentModel>(
            (students) => students.Where((std) =>
            {
                var result = std.CurrentSpeciality;
                if (result is null){
                    return false;
                } 
                return result.EducationalLevelIn.LevelCode == LevelsOfEducation.SecondaryGeneralEducation;
            })
        );
        var midEduCell2 = new ColumnHeaderCell<StudentModel>(
            "На базе среднего общего образования",
            trTypeCell1,
            midEduFilter2
        );

        // 3 уровень
        // на базе основного общего
        var enlistedFilter3 = new Filter<StudentModel>(
            (students) => students.Where(std =>
            {
                return std.History.IsEnlistedInStandardPeriod();
            })
        );
        var enlistedWomanFilter3 = new Filter<StudentModel>(
            (students) => students.Where(std =>
            {
                return std.Gender == Genders.GenderCodes.Female;
            })
        ).Include(enlistedFilter3);

        var studyingFilter3 = new Filter<StudentModel>(
            (students) => students.Where(std =>
            {
                return std.History.IsStudentEnlisted() && std.CurrentGroup.FormatOfEducation.FormatType == GroupEducationFormatTypes.FullTime;
            })
        );
        var studyingWomanFilter3 = new Filter<StudentModel>(
            (students) => students.Where(std =>
            {
                return std.Gender == Genders.GenderCodes.Female;
            })
        ).Include(studyingFilter3); 
        
        var enlistedCell3 = new ColumnHeaderCell<StudentModel>(
            "Принято",
            baseEduCell2,
            enlistedFilter3
        );
        var enlistedWomanCell3 = new ColumnHeaderCell<StudentModel>(
            "Из них женщины",
            baseEduCell2,
            enlistedWomanFilter3
        );
        var studyingCell3 = new ColumnHeaderCell<StudentModel>(
            "Численность студентов",
            baseEduCell2,
            studyingFilter3
        );
        // студенты выпускных курсов
        var graduatesFilter3 = new Filter<StudentModel>(
            (students) => students.Where(
                std => std.CurrentGroup.CourseOn == std.CurrentSpeciality.CourseCount
            )
        );
        var graduatesFemaleFilter3 = new Filter<StudentModel>(
            (students) => students.Where(
                std => std.IsFemale
            )
        ).Include(graduatesFilter3);

        var graduatedCell3 = new ColumnHeaderCell<StudentModel>(
            "Планируемый выпуск",
            baseEduCell2,
            graduatesFilter3
        );
        var graduatedFemaleCell3 = new ColumnHeaderCell<StudentModel>(
            "Из них женщины",
            baseEduCell2,
            graduatesFemaleFilter3
        );
        
        // 2 часть 3 уровень
        var elnistedSecondCell3 = new ColumnHeaderCell<StudentModel>(
            "Принято",
            midEduCell2,
            enlistedFilter3
        );
        var enlistedWomanSecondCell3 = new ColumnHeaderCell<StudentModel>(
            "Из них женщины",
            midEduCell2,
            enlistedWomanFilter3
        );
        var studySecondCell3 = new ColumnHeaderCell<StudentModel>(
            "Численность студентов",
            midEduCell2,
            studyingFilter3
        );
        var studyingFemaleSecondCell3 = new ColumnHeaderCell<StudentModel>(
            "Из них женщины",
            midEduCell2,
            graduatesFilter3
        );
        var graduatedSecondCell3 = new ColumnHeaderCell<StudentModel>(
            "Планируемый выпуск",
            midEduCell2,
            graduatesFilter3
        );
        var graduatedFemaleSecondCell3 = new ColumnHeaderCell<StudentModel>(
            "Из них женщины",
            midEduCell2,
            graduatesFemaleFilter3
        );

        var horizontalRoot = new RowHeaderCell<StudentModel>();
        var ageDate = new DateTime(DateTime.Now.Year, 1, 1); 
        var youngerThan13Filter = new Filter<StudentModel>(
            (students) => students.Where(std => std.GetAgeOnDate(ageDate) < 13)
        );
        var youngerThan13Cell = new RowHeaderCell<StudentModel>(
            "Моложе 13 лет",
            horizontalRoot,
            youngerThan13Filter
        );
        for (int i = 14; i <= 30; i++){
            var ageFilter = new Filter<StudentModel>(
                (students) => students.Where(std => std.GetAgeOnDate(ageDate) == i + 0)
            );
            var ageCell = new RowHeaderCell<StudentModel>(
                (i + 0).ToString() + " лет",
                horizontalRoot,
                ageFilter
            );
        }
        var found = StudentModel.FindUniqueStudents(new QueryLimits(0, 2000)).Result;
        var verticalHeader = new TableColumnHeader<StudentModel>(verticalRoot, true);
        var horizontalHeader = new TableRowHeader<StudentModel>(horizontalRoot, verticalHeader, true);
        var table = new StatisticTable<StudentModel>(verticalHeader, horizontalHeader, found);

        return View(@"Views/Statistics/Ages.cshtml", table);

    }
}
