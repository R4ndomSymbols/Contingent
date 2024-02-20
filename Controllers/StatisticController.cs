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
        var parameters = new SQLParameterCollection();
        var p1 = parameters.Add((int)LevelsOfEducation.BasicGeneralEducation);
        var p2 = parameters.Add((int)LevelsOfEducation.SecondaryGeneralEducation);
        var p3type = NpgsqlDbType.Array | NpgsqlDbType.Integer;
        var p3 = parameters.Add(OrderTypeInfo.GetAllEnrollment().Select(x => (int)x.Type).ToArray(), p3type);         

        var verticalRoot = new ColumnHeaderCell<StudentModel>();
        /*
        var specialitiesFilter = new ConstrainedColumnHeaderCell("Программы подготовки специалистов среднего звена",
            new ComplexWhereCondition(
            new WhereCondition(
            new Column("specialities", "specialities"),  WhereCondition.Relations.Equal,))
        */
        // 1 уровень
        
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
        var midEduCell = new ColumnHeaderCell<StudentModel>(
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
        ).Merge(enlistedFilter3);

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
        ).Merge(studyingFilter3); 
        
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














        var studyingFirstCourse = new Filter<StudentModel>(
            (students) => students.Where(std =>
            {
                return std.CurrentGroup.CourseOn == 1;
            })
        ).Merge(studyingFilter);



       


    
    }


}



