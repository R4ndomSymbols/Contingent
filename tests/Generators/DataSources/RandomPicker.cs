namespace Tests;

public class RandomPicker<T> {

    private static readonly Random gen = new Random(712394);
    public static T Pick(IReadOnlyList<T> items){
        return items[gen.Next(0, items.Count)];
    }


}