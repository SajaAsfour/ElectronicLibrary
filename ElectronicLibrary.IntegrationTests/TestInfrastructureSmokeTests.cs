using System.Reflection;

namespace ElectronicLibrary.IntegrationTests;

public class TestInfrastructureSmokeTests
{
    [Fact]
    public void IntegrationTestProject_CanLoadPresentationLayer()
    {
        Assembly assembly = Assembly.Load("ElectronicLibrary.PL");

        Assert.Equal("ElectronicLibrary.PL", assembly.GetName().Name);
    }
}