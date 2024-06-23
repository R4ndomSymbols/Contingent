using System.Collections;

namespace Contingent.Models.Domain.Flow.Abstract;

public abstract class OrderedHistory : IEnumerable<StudentFlowRecord>
{
    protected List<StudentFlowRecord> _history;
    protected OrderedHistory()
    {
        _history = new List<StudentFlowRecord>();
    }
    protected OrderedHistory(IEnumerable<StudentFlowRecord> records) : this()
    {
        _history.AddRange(records);
    }

    public IEnumerator<StudentFlowRecord> GetEnumerator()
    {
        return _history.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return _history.GetEnumerator();
    }

    public abstract void Add(StudentFlowRecord record);
    public abstract StudentFlowRecord? Last();
    // запись, которая соответствует состоянию на дату
    public abstract StudentFlowRecord? On(DateTime onDate);


}