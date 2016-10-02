using System;

namespace StandardContainer.Tests.Utilities
{
    public static class TestExtensions
    {
        public static Exception Output(this Exception ex, Action<string> write)
        {
            write(ex.Message);
            return ex;
        }
    }
}
