using System;

namespace StandardContainer.Tests
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
