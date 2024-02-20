using Microsoft.AspNetCore.Mvc;
using StudentTracking.Statistics;
using StudentTracking.SQL;
using StudentTracking.Models;
using StudentTracking.Models.Domain.Misc;
using NpgsqlTypes;
using StudentTracking.Models.Domain.Orders.OrderData;

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
        var p4 =         


        var verticalRoot = new ConstrainedColumnHeaderCell(parameters);
        /*
        var specialitiesFilter = new ConstrainedColumnHeaderCell("Программы подготовки специалистов среднего звена",
            new ComplexWhereCondition(
            new WhereCondition(
            new Column("specialities", "specialities"),  WhereCondition.Relations.Equal,))
        */

        // 2 уровень
        var baseEduFilter = new ConstrainedColumnHeaderCell(
        "на базе основного общего образования",
        new ComplexWhereCondition(
            new WhereCondition(
                new Column("specialities", "educational_level_in"), p1, WhereCondition.Relations.Equal
                )
            ), ComplexWhereCondition.ConditionRelation.AND, verticalRoot);

        var fullEduFilter = new ConstrainedColumnHeaderCell(
        "на базе среднего общего образования",
        new ComplexWhereCondition(
            new WhereCondition(
                new Column("specialities", "educational_level_in"), p2, WhereCondition.Relations.Equal
                )
            ), ComplexWhereCondition.ConditionRelation.AND, verticalRoot);
        // 3 уровень
        var fullEduFilter = new ConstrainedColumnHeaderCell(
        "Принято",
        new ComplexWhereCondition(
            new WhereCondition(
                new Column("orders", "order_type"), p2, WhereCondition.Relations.InArray
                )
            ), ComplexWhereCondition.ConditionRelation.AND, verticalRoot);
        ""



    
    }


}



