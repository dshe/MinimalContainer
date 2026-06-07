namespace MinimalContainer.Tests;

public abstract class BaseUnitTest
{
    protected readonly Action<string> Log;

    public BaseUnitTest(ITestOutputHelper output)
    {
        Log = output.WriteLine;
    }
}
