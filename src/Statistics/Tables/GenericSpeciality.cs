using Contingent.Models.Domain.Flow;
using Contingent.Models.Domain.Orders.OrderData;
using Contingent.Models.Domain.Students;
using Contingent.Models.Infrastructure;
using Contingent.Statistics.Tables.Headers;

namespace Contingent.Statistics.Tables;

public class GenericSpecialty : ITable
{
    private StatisticTable<StudentModel> _model;
    public string DisplayedName => _model.TableName;
    public string HtmlContent => _model.ToHtmlTable();
    public Period StatisticPeriod { get; set; }

    public GenericSpecialty(Period statsPeriod)
    {
        if (!statsPeriod.IsOneMoment())
        {
            StatisticPeriod = new Period(statsPeriod.End, statsPeriod.End);
        }
        else
        {
            StatisticPeriod = statsPeriod;
        }
        var verticalRoot = new ColumnHeaderCell<StudentModel>();
        var rowHeaderColHeader = new ColumnHeaderCell<StudentModel>(
            "Специальности",
            verticalRoot
        );
        for (int i = 1; i < 5; i++)
        {
            var first = TemplateHeaders.GetBaseCourseHeader(
            i,
            (StudentModel s) => s,
            (StudentModel s) => s.GetHistory(null, statsPeriod.End).GetGroupOnDate(StatisticPeriod.End),
            verticalRoot
            );
        }
        var verticalHeader = new TableColumnHeader<StudentModel>(verticalRoot, false);
        var horizontalHeader = TemplateHeaders.GetSpecialtyRowHeader(
            (StudentModel s) => s.GetHistory(null, statsPeriod.End)
            .GetGroupOnDate(StatisticPeriod.End)?.EducationProgram,
            verticalHeader
        );
        var source = StudentHistory.GetStudentByOrderState(StatisticPeriod.End,
        OrderTypeInfo.EnrollmentTypes,
        OrderTypeInfo.DeductionTypes,
        null);
        _model = new StatisticTable<StudentModel>(
            verticalHeader,
            horizontalHeader,
            source,
            "Специальности"
        );
    }


}
