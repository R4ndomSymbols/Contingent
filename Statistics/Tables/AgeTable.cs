using StudentTracking.Models.Domain.Flow;
using StudentTracking.Models.Domain.Misc;
using StudentTracking.SQL;

namespace StudentTracking.Statistics.Tables;

public class AgeTable : ITable
{
    private StatisticTable<StudentFlowRecord> _model;
    public string DisplayedName => _model.TableName;
    public string Html => _model.ToHtmlTable();
    public AgeTable()
    {
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
                    rec =>
                    {

                        var result = rec.GroupTo?.EducationProgram;
                        if (result is null)
                        {
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
                if (result is null)
                {
                    return false;
                }
                return result.EducationalLevelIn.LevelCode == LevelsOfEducation.BasicGeneralEducation;
            })
        );
        var baseEduCell2 = new ColumnHeaderCell<StudentFlowRecord>(
            "На базе основного общего образования",
            trTypeCell1,
            baseEduFilter2
        );
        var midEduFilter2 = new Filter<StudentFlowRecord>(
            (students) => students.Where((std) =>
            {

                var result = std.GroupTo?.EducationProgram;
                if (result is null)
                {
                    return false;
                }
                return result.EducationalLevelIn.LevelCode == LevelsOfEducation.SecondaryGeneralEducation;
            })
        );
        var midEduCell2 = new ColumnHeaderCell<StudentFlowRecord>(
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
        var enlistedWomanFilter3 = new Filter<StudentFlowRecord>(
            (students) => students.Where(std =>
            {
                return std.Student.Gender == Genders.GenderCodes.Female;
            })
        ).Include(enlistedFilter3);

        var studyingFilter3 = new Filter<StudentFlowRecord>(
            (students) => students.Where(std =>
            {
                return std.Student.History.IsStudentEnlisted() && std.GroupTo?.FormatOfEducation.FormatType == GroupEducationFormatTypes.FullTime;
            })
        );
        var studyingWomanFilter3 = new Filter<StudentFlowRecord>(
            (students) => students.Where(std =>
            {
                return std.Student.Gender == Genders.GenderCodes.Female;
            })
        ).Include(studyingFilter3);

        var enlistedCell3 = new ColumnHeaderCell<StudentFlowRecord>(
            "Принято",
            baseEduCell2,
            enlistedFilter3
        );
        var enlistedWomanCell3 = new ColumnHeaderCell<StudentFlowRecord>(
            "Из них женщины",
            baseEduCell2,
            enlistedWomanFilter3
        );
        var studyingCell3 = new ColumnHeaderCell<StudentFlowRecord>(
            "Численность студентов",
            baseEduCell2,
            studyingFilter3
        );
        // студенты выпускных курсов
        var graduatesFilter3 = new Filter<StudentFlowRecord>(
            (students) => students.Where(

                std => std.GroupTo?.CourseOn == std.GroupTo?.EducationProgram.CourseCount
            )
        );
        var graduatesFemaleFilter3 = new Filter<StudentFlowRecord>(
            (students) => students.Where(
                std => std.Student.IsFemale
            )
        ).Include(graduatesFilter3);

        var graduatedCell3 = new ColumnHeaderCell<StudentFlowRecord>(
            "Планируемый выпуск",
            baseEduCell2,
            graduatesFilter3
        );
        var graduatedFemaleCell3 = new ColumnHeaderCell<StudentFlowRecord>(
            "Из них женщины",
            baseEduCell2,
            graduatesFemaleFilter3
        );

        // 2 часть 3 уровень
        var elnistedSecondCell3 = new ColumnHeaderCell<StudentFlowRecord>(
            "Принято",
            midEduCell2,
            enlistedFilter3
        );
        var enlistedWomanSecondCell3 = new ColumnHeaderCell<StudentFlowRecord>(
            "Из них женщины",
            midEduCell2,
            enlistedWomanFilter3
        );
        var studySecondCell3 = new ColumnHeaderCell<StudentFlowRecord>(
            "Численность студентов",
            midEduCell2,
            studyingFilter3
        );
        var studyingFemaleSecondCell3 = new ColumnHeaderCell<StudentFlowRecord>(
            "Из них женщины",
            midEduCell2,
            graduatesFilter3
        );
        var graduatedSecondCell3 = new ColumnHeaderCell<StudentFlowRecord>(
            "Планируемый выпуск",
            midEduCell2,
            graduatesFilter3
        );
        var graduatedFemaleSecondCell3 = new ColumnHeaderCell<StudentFlowRecord>(
            "Из них женщины",
            midEduCell2,
            graduatesFemaleFilter3
        );

        var horizontalRoot = new RowHeaderCell<StudentFlowRecord>();
        var ageDate = new DateTime(DateTime.Now.Year, 1, 1);
        var youngerThan13Filter = new Filter<StudentFlowRecord>(
            (students) => students.Where(std =>
            {
                return std.Student.GetAgeOnDate(ageDate) < 13;
            })
        );
        var youngerThan13Cell = new RowHeaderCell<StudentFlowRecord>(
            "Моложе 13 лет",
            horizontalRoot,
            youngerThan13Filter
        );
        for (int i = 14; i <= 30; i++)
        {
            var scoped = i;
            var ageFilter = new Filter<StudentFlowRecord>(
                (students) => students.Where(std =>
                {
                    return std.Student.GetAgeOnDate(ageDate) == scoped;
                })
            );
            var ageCell = new RowHeaderCell<StudentFlowRecord>(
                scoped.ToString() + " лет",
                horizontalRoot,
                ageFilter
            );
        }
        var found = StudentHistory.GetLastRecordsForManyStudents(new QueryLimits(0, 2000), (false, false));
        var verticalHeader = new TableColumnHeader<StudentFlowRecord>(verticalRoot, true);
        var horizontalHeader = new TableRowHeader<StudentFlowRecord>(horizontalRoot, verticalHeader, true);
        _model = new StatisticTable<StudentFlowRecord>(verticalHeader, horizontalHeader, found, "Характеристика контингента");
    }

}