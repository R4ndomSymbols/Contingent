namespace Contingent.Utilities;
public static class ResultHelper
{


    public static bool AnyFailure(params IResult[] results)
    {
        return results.Any(x => x.IsFailure);
    }

    public static IReadOnlyCollection<ValidationError> MergeErrors(params IResult[] results)
    {
        var collection = new List<ValidationError>();
        foreach (var r in results)
        {
            if (r.IsFailure)
            {
                collection.AddRange(r.GetErrors());
            }
        }
        return collection;
    }
}
