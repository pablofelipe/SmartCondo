namespace SmartCondoApi.Tests
{
    [TestClass]
    public class StartupHostingModeTest
    {
        [TestMethod]
        public void IsLambdaHosted_VariableSet_ReturnsTrue()
        {
            Assert.IsTrue(Startup.IsLambdaHosted("my-function"));
        }

        [TestMethod]
        public void IsLambdaHosted_VariableNullOrEmpty_ReturnsFalse()
        {
            Assert.IsFalse(Startup.IsLambdaHosted(null));
            Assert.IsFalse(Startup.IsLambdaHosted(""));
        }
    }
}
