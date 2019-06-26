using Microsoft.Extensions.Logging;
using MXLogger;
using Xunit.Abstractions;

namespace MinimalContainer.Tests.Utility
{
    public abstract class BaseUnitTest
    {
        protected readonly ILogger<Container> Logger;

        public BaseUnitTest(ITestOutputHelper output)
        {
            var factory = new LoggerFactory().AddMXLogger(output.WriteLine);
            Logger = factory.CreateLogger<Container>();
        }
    }
}
