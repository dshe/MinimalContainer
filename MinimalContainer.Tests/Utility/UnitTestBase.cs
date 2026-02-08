using System;
using Xunit;

namespace MinimalContainer.Tests.Utility
{
    public abstract class BaseUnitTest
    {
        protected readonly Action<string> Log;

        public BaseUnitTest(ITestOutputHelper output)
        {
            Log = output.WriteLine;
        }
    }
}
