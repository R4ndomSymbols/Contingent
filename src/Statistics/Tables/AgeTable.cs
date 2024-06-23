using Contingent.Models.Domain.Flow;
using Contingent.Models.Domain.Students;
using Contingent.Models.Domain.Groups;
using Contingent.Models.Domain.Specialties;
using Contingent.SQL;
using Contingent.Models.Infrastructure;
using Microsoft.AspNetCore.Http.Features;
using Contingent.Models.Domain.Orders.OrderData;

namespace Contingent.Statistics.Tables;

// таблица возрастов
public class AgeTable : ITable
{
    private StatisticTable<StudentModel> _model;
    public string DisplayedName => _model.TableName;
    public string HtmlContent => _model.ToHtmlTable();
    public Period StatisticPeriod { get; set; }
    public AgeTable(TrainingProgramTypes type, Period statsPeriod)
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
            (recs) =>
                recs.Where(
                    student =>
                    {

                        var result = student.GetHistory(null, StatisticPeriod.End).GetGroupOnDate(StatisticPeriod.End);
                        if (result is null)
                        {
                            return false;
                        }
                        return result.EducationProgram.ProgramType.Type == TrainingProgramTypes.QualifiedWorker;
                    }
                )
        );

        ColumnHeaderCell<StudentModel>? trTypeCell1 = null;
        switch (type)
        {
            case TrainingProgramTypes.QualifiedWorker:
                trTypeCell1 = new ColumnHeaderCell<StudentModel>(
                    "Квалифицированные рабочие, служащие",
                    verticalRoot,
                    new Filter<StudentModel>(
                    (recs) =>
                        recs.Where(
                            student =>
                            {

                                var result = student.GetHistory(null, StatisticPeriod.End).GetGroupOnDate(StatisticPeriod.Start);
                                if (result is null)
                                {
                                    return false;
                                }
                                return result.EducationProgram.ProgramType.IsQualifiedWorker();
                            }
                        )));
                break;
            case TrainingProgramTypes.GenericSpecialist:
                trTypeCell1 = new ColumnHeaderCell<StudentModel>(
                     "Специалисты среднего звена",
                     verticalRoot,
                     new Filter<StudentModel>(
                     (recs) =>
                         recs.Where(
                             student =>
                             {

                                 var result = student.GetHistory(null, StatisticPeriod.End).GetGroupOnDate(StatisticPeriod.Start);
                                 if (result is null)
                                 {
                                     return false;
                                 }
                                 return result.EducationProgram.ProgramType.IsGenericSpecialist();
                             }
                         )));
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(type), type, null);
        }
        // 2 уровень
        var baseEduFilter2 = new Filter<StudentModel>(
            (students) => students.Where((std) =>
            {

                var result = std.GetHistory(null, StatisticPeriod.End).
                    GetGroupOnDate(StatisticPeriod.Start)?.EducationProgram;
                if (result is null)
                {
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

                var result = std.GetHistory(null, StatisticPeriod.End)
                    .GetGroupOnDate(StatisticPeriod.Start)?
                    .EducationProgram;
                if (result is null)
                {
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
        var womanFilter3 = new Filter<StudentModel>(
            (students) => students.Where(std =>
            {
                return std.IsFemale;
            })
        );

        var studyingCell3 = new ColumnHeaderCell<StudentModel>(
            "Численность студентов",
            baseEduCell2,
            Filter<StudentModel>.Empty
        );
        var enlistedWomanCell3 = new ColumnHeaderCell<StudentModel>(
            "Из них женщины",
            baseEduCell2,
            womanFilter3
        );

        // студенты выпускных курсов
        var graduatesFilter3 = new Filter<StudentModel>(
            (students) => students.Where(
                (std) =>
                {
                    var group = std.GetHistory(null, StatisticPeriod.End).GetGroupOnDate(StatisticPeriod.Start);
                    if (group is null)
                    {
                        return false;
                    }
                    return group.EducationProgram.CourseCount == group.CourseOn;
                }
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
        var enlistedSecondCell3 = new ColumnHeaderCell<StudentModel>(
            "Принято",
            midEduCell2,
            Filter<StudentModel>.Empty
        );
        var enlistedWomanSecondCell3 = new ColumnHeaderCell<StudentModel>(
            "Из них женщины",
            midEduCell2,
            womanFilter3
        );
        var studySecondCell3 = new ColumnHeaderCell<StudentModel>(
            "Численность студентов",
            midEduCell2,
            Filter<StudentModel>.Empty
        );
        var studyingFemaleSecondCell3 = new ColumnHeaderCell<StudentModel>(
            "Из них женщины",
            midEduCell2,
            womanFilter3
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
        var ageDate = StatisticPeriod.End;
        var youngerThan13Filter = new Filter<StudentModel>(
            (students) => students.Where(std =>
            {
                return std.GetAgeOnDate(ageDate) < 13;
            })
        );
        var youngerThan13Cell = new RowHeaderCell<StudentModel>(
            "Моложе 13 лет",
            horizontalRoot,
            youngerThan13Filter
        );
        for (int i = 14; i <= 30; i++)
        {
            var scoped = i;
            var ageFilter = new Filter<StudentModel>(
                (students) => students.Where(std =>
                {
                    return std.GetAgeOnDate(ageDate) == scoped;
                })
            );
            var ageCell = new RowHeaderCell<StudentModel>(
                scoped.ToString() + " лет",
                horizontalRoot,
                ageFilter
            );
        }
        var finalAgeCell = new RowHeaderCell<StudentModel>(
            "Старше 30 лет",
            horizontalRoot,
            new Filter<StudentModel>(
                (students) => students.Where(std => std.GetAgeOnDate(ageDate) > 30)
            )
        );
        var source = StudentHistory.GetStudentByOrderState(StatisticPeriod.End,
            OrderTypeInfo.EnrollmentTypes,
            OrderTypeInfo.DeductionTypes,
        null);
        var verticalHeader = new TableColumnHeader<StudentModel>(verticalRoot, true);
        var horizontalHeader = new TableRowHeader<StudentModel>(horizontalRoot, verticalHeader, true);
        _model = new StatisticTable<StudentModel>(verticalHeader, horizontalHeader, source, "Характеристика контингента");
    }
}