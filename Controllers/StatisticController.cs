using Microsoft.AspNetCore.Mvc;
using StudentTracking.Statistics;
using StudentTracking.SQL;
using StudentTracking.Models.Domain.Misc;
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
              
        var verticalRoot = new ColumnHeaderCell<StudentFlowRecord>();
        /*
        var specialitiesFilter = new ConstrainedColumnHeaderCell("Программы подготовки специалистов среднего звена",
            new ComplexWhereCondition(
            new WhereCondition(
            new Column("specialities", "specialities"),  WhereCondition.Relations.Equal,))
        */
        // 1 уровень
        var parameterNameCell1 = new ColumnHeaderCell<StudentFlowRecord>(
            "Наименование показателей",
            verticalRoot
        );
        var rowNumberCell = new ColumnHeaderCell<StudentFlowRecord>(
            "№ строки",
            verticalRoot
        );
        
        var trTypeFilter1 = new Filter<StudentFlowRecord>(
            (recs) => 
                recs.Where(
                    rec =>{
                        var result = rec.GroupTo?.EducationProgram;
                        if (result is null){
                            return false;
                        } 
                        return result.ProgramType.Type == TrainingProgramTypes.QualifiedWorker;
                    } 
                )
        );
        var trTypeCell1 = new ColumnHeaderCell<StudentFlowRecord>(
            "Программы подготовки квалифицированных рабочих, служащих",
            verticalRoot,
            trTypeFilter1);
        // 2 уровень
        var baseEduFilter2 = new Filter<StudentFlowRecord>(
            (students) => students.Where((std) =>
            {
                var result = std.GroupTo?.EducationProgram;
                if (result is null){
                    return false;
                } 
                return result.EducationalLevelIn.LevelCode == LevelsOfEducation.BasicGeneralEducation;
            })
        );
        var baseEduCell2 = new ColumnHeaderCell< StudentFlowRecord>(
            "На базе основного общего образования",
            trTypeCell1,
            baseEduFilter2
        );
        var midEduFilter2 = new Filter< StudentFlowRecord>(
            (students) => students.Where((std) =>
            {
                var result = std.GroupTo?.EducationProgram;
                if (result is null){
                    return false;
                } 
                return result.EducationalLevelIn.LevelCode == LevelsOfEducation.SecondaryGeneralEducation;
            })
        );
        var midEduCell2 = new ColumnHeaderCell< StudentFlowRecord>(
            "На базе среднего общего образования",
            trTypeCell1,
            midEduFilter2
        );

        // 3 уровень
        // на базе основного общего
        var enlistedFilter3 = new Filter<StudentFlowRecord>(
            (students) => students.Where(std =>
            {
                return std.Student.History.IsEnlistedInStandardPeriod();
            })
        );
        var enlistedWomanFilter3 = new Filter< StudentFlowRecord>(
            (students) => students.Where(std =>
            {
                return std.Student.Gender == Genders.GenderCodes.Female;
            })
        ).Include(enlistedFilter3);

        var studyingFilter3 = new Filter< StudentFlowRecord>(
            (students) => students.Where(std =>
            {
                return std.Student.History.IsStudentEnlisted() && std.GroupTo?.FormatOfEducation.FormatType == GroupEducationFormatTypes.FullTime;
            })
        );
        var studyingWomanFilter3 = new Filter< StudentFlowRecord>(
            (students) => students.Where(std =>
            {
                return std.Student.Gender == Genders.GenderCodes.Female;
            })
        ).Include(studyingFilter3); 
        
        var enlistedCell3 = new ColumnHeaderCell< StudentFlowRecord>(
            "Принято",
            baseEduCell2,
            enlistedFilter3
        );
        var enlistedWomanCell3 = new ColumnHeaderCell< StudentFlowRecord>(
            "Из них женщины",
            baseEduCell2,
            enlistedWomanFilter3
        );
        var studyingCell3 = new ColumnHeaderCell< StudentFlowRecord>(
            "Численность студентов",
            baseEduCell2,
            studyingFilter3
        );
        // студенты выпускных курсов
        var graduatesFilter3 = new Filter< StudentFlowRecord>(
            (students) => students.Where(
                std => std.GroupTo?.CourseOn == std.GroupTo?.EducationProgram.CourseCount
            )
        );
        var graduatesFemaleFilter3 = new Filter< StudentFlowRecord>(
            (students) => students.Where(
                std => std.Student.IsFemale
            )
        ).Include(graduatesFilter3);

        var graduatedCell3 = new ColumnHeaderCell< StudentFlowRecord>(
            "Планируемый выпуск",
            baseEduCell2,
            graduatesFilter3
        );
        var graduatedFemaleCell3 = new ColumnHeaderCell< StudentFlowRecord>(
            "Из них женщины",
            baseEduCell2,
            graduatesFemaleFilter3
        );
        
        // 2 часть 3 уровень
        var elnistedSecondCell3 = new ColumnHeaderCell< StudentFlowRecord>(
            "Принято",
            midEduCell2,
            enlistedFilter3
        );
        var enlistedWomanSecondCell3 = new ColumnHeaderCell< StudentFlowRecord>(
            "Из них женщины",
            midEduCell2,
            enlistedWomanFilter3
        );
        var studySecondCell3 = new ColumnHeaderCell< StudentFlowRecord>(
            "Численность студентов",
            midEduCell2,
            studyingFilter3
        );
        var studyingFemaleSecondCell3 = new ColumnHeaderCell< StudentFlowRecord>(
            "Из них женщины",
            midEduCell2,
            graduatesFilter3
        );
        var graduatedSecondCell3 = new ColumnHeaderCell< StudentFlowRecord>(
            "Планируемый выпуск",
            midEduCell2,
            graduatesFilter3
        );
        var graduatedFemaleSecondCell3 = new ColumnHeaderCell< StudentFlowRecord>(
            "Из них женщины",
            midEduCell2,
            graduatesFemaleFilter3
        );

        var horizontalRoot = new RowHeaderCell< StudentFlowRecord>();
        var ageDate = new DateTime(DateTime.Now.Year, 1, 1); 
        var youngerThan13Filter = new Filter< StudentFlowRecord>(
            (students) => students.Where(std => std.Student.GetAgeOnDate(ageDate) < 13)
        );
        var youngerThan13Cell = new RowHeaderCell< StudentFlowRecord>(
            "Моложе 13 лет",
            horizontalRoot,
            youngerThan13Filter
        );
        for (int i = 14; i <= 30; i++){
            var ageFilter = new Filter< StudentFlowRecord>(
                (students) => students.Where(std => std.Student.GetAgeOnDate(ageDate) == i + 0)
            );
            var ageCell = new RowHeaderCell< StudentFlowRecord>(
                (i + 0).ToString() + " лет",
                horizontalRoot,
                ageFilter
            );
        }
        var found = StudentHistory.GetLastRecordsForManyStudents(new QueryLimits(0, 2000));
        var verticalHeader = new TableColumnHeader< StudentFlowRecord>(verticalRoot, true);
        var horizontalHeader = new TableRowHeader< StudentFlowRecord>(horizontalRoot, verticalHeader, true);
        var table = new StatisticTable< StudentFlowRecord>(verticalHeader, horizontalHeader, found, "Характеристика контингента");

        return View(@"Views/Statistics/Ages.cshtml", table);

    }
}
