using LexCore.Utils;
using Shouldly;

namespace Testing.LexCore.Utils;

public class ConcurrentWeakDictionaryTests
{

    [Fact]
    public void Add_Then_Try_Get_Value_Test()
    {
        var obj = new object();
        var dict = new ConcurrentWeakDictionary<string, object>();
        dict.Add("key", obj);
        dict.TryGetValue("key", out var value).ShouldBeTrue();
        value.ShouldBe(obj);
    }

    [Fact]
    public void GetOrAdd_New_Key_Should_Add_And_Return_New_Value_Test()
    {
        var value = new object();

        var dictionary = new ConcurrentWeakDictionary<string, object>();

        var returnedValue = dictionary.GetOrAdd("key", k => value);

        returnedValue.ShouldBe(value);
        dictionary.TryGetValue("key", out var existingValue).ShouldBeTrue();
        existingValue.ShouldBe(value);
    }

    [Fact]
    public void GetOrAdd_Existing_Key_Should_Return_Existing_Value_Test()
    {
        var key = "key";
        var value = new object();

        var dictionary = new ConcurrentWeakDictionary<string, object>();
        dictionary.Add(key, value);

        var returnedValue = dictionary.GetOrAdd(key, k => new object());

        returnedValue.ShouldBe(value);
        dictionary.TryGetValue(key, out var existingValue).ShouldBeTrue();
        existingValue.ShouldBe(value);
    }

    private ConcurrentWeakDictionary<string, object> Setup(string key)
    {
        var value = new object();

        var dictionary = new ConcurrentWeakDictionary<string, object>();
        dictionary.Add(key, value);

        // Allow GC to collect the value.
        value = null;
        return dictionary;
    }

    [Fact]
    public void Add_Then_Collect_And_Check_That_Key_Is_Removed_Test()
    {
        var key = "key";
        //need to do setup in another method otherwise dotnet won't cleanup the value from GC until the end of the test
        //https://stackoverflow.com/a/58662332/1620542
        var dictionary = Setup(key);
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Aggressive, true, true);
        GC.WaitForPendingFinalizers();

        // Check that the value for the key no longer exists.
        dictionary.TryGetValue(key, out var result).ShouldBeFalse();
    }
}
