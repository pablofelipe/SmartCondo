using Microsoft.Extensions.Logging;

namespace SmartCondoApi.Tests
{
    [TestClass]
    public class StartupLoggingTest
    {
        [TestMethod]
        public void ResolveMinimumLogLevel_Development_ReturnsDebug()
        {
            var result = Startup.ResolveMinimumLogLevel(isDevelopment: true);

            Assert.AreEqual(LogLevel.Debug, result);
        }

        [TestMethod]
        public void ResolveMinimumLogLevel_NotDevelopment_ReturnsInformation()
        {
            var result = Startup.ResolveMinimumLogLevel(isDevelopment: false);

            Assert.AreEqual(LogLevel.Information, result);
        }
    }
}
