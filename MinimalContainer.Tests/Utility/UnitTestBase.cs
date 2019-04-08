using Divergic.Logging.Xunit;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace MinimalContainer.Tests.Utility
{
    public abstract class BaseUnitTest
    {
        protected readonly ILogger<Container> Logger;

        public BaseUnitTest(ITestOutputHelper output)
        {
            Logger = output.BuildLoggerFor<Container>(); // Divergic.Logging.Xunit
        }
    }
}
