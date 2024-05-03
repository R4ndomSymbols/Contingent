using Contingent.Models.Domain.Flow;
using Contingent.Statistics.Tables.Headers;

namespace Contingent.Statistics.Tables;

public class GenericSpeciality : ITable
{
    private StatisticTable<StudentFlowRecord> _model;
    public string DisplayedName => _model.TableName;
    public string Html => _model.ToHtmlTable();


    /*



    */


    public GenericSpeciality()
    {
        var verticalRoot = new ColumnHeaderCell<StudentFlowRecord>();
        var rowHeaderColHeader = new ColumnHeaderCell<StudentFlowRecord>(
            "Специальности",
            verticalRoot
        );
        for (int i = 1; i < 5; i++)
        {
            var first = TemplateHeaders.GetBaseCourseHeader(
            i,
            (StudentFlowRecord s) => s.Student,
            (StudentFlowRecord s) => s.GroupTo,
            verticalRoot
            );
        }
        var verticalHeader = new TableColumnHeader<StudentFlowRecord>(verticalRoot, false);
        var horizontalHeader = TemplateHeaders.GetSpecialityRowHeader(
            (StudentFlowRecord s) => s.GroupTo?.EducationProgram,
            verticalHeader
        );
        var source = StudentHistory.GetLastRecordsForManyStudents(new SQL.QueryLimits(0, 2000), (false, false));
        _model = new StatisticTable<StudentFlowRecord>(
            verticalHeader,
            horizontalHeader,
            source,
            "Специальности"
        );
    }


}
