using Divergic.Logging.Xunit;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace MinimalContainer.Tests.Utility
{
    public abstract class UnitTestBase
    {
        protected readonly ILogger<Container> Logger;

        public UnitTestBase(ITestOutputHelper output)
        {
            Logger = output.BuildLoggerFor<Container>(); // Divergic.Logging.Xunit
        }
    }
}
