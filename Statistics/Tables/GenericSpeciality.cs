using Microsoft.AspNetCore.Authorization.Infrastructure;
using StudentTracking.Models.Domain;
using StudentTracking.Models.Domain.Flow;
using StudentTracking.Statistics.Tables.Headers;

namespace StudentTracking.Statistics.Tables;

public class GenericSpeciality : ITable
{
    private StatisticTable<StudentFlowRecord> _model; 
    public string DisplayedName => _model.TableName;
    public string Html => _model.ToHtmlTable();
   

    /*



    */


    public GenericSpeciality(){
        var verticalRoot = new ColumnHeaderCell<StudentFlowRecord>();
        var rowHeaderColHeader = new ColumnHeaderCell<StudentFlowRecord>(
            "Специальности",
            verticalRoot
        );
        for(int i = 1; i < 5; i++){
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
        var source = StudentHistory.GetLastRecordsForManyStudents(new SQL.QueryLimits(0, 2000));
        _model = new StatisticTable<StudentFlowRecord>(
            verticalHeader,
            horizontalHeader,
            source,
            "Специальности" 
        ); 
    }

    
}
