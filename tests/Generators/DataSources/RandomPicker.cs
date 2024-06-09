using Xunit.Sdk;

namespace Tests;

public class RandomPicker<T>
{
    private static void UpdateCount()
    {
        if (_count % 100 == 0)
        {
            gen = new Random();
        }
    }
    private static int _count;
    private static Random gen = new();
    public static T Pick(IList<T> items)
    {
        UpdateCount();
        return items[gen.Next(0, items.Count)];
    }


}