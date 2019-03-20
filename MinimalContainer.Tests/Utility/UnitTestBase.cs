using Divergic.Logging.Xunit;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace MinimalContainer.Tests.Utility
{
    public abstract class TestBase
    {
        protected readonly ILogger Logger;
        protected readonly ILoggerFactory LoggerFactory;

        public TestBase(ITestOutputHelper output)
        {
            Logger = output.BuildLogger("TestBase"); // Divergic.Logging.Xunit
            LoggerFactory = LogFactory.Create(output); // Divergic.Logging.Xunit
        }
    }
}
