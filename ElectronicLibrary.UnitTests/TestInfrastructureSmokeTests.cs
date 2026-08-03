using System.Reflection;

namespace ElectronicLibrary.UnitTests;

public class TestInfrastructureSmokeTests
{
    [Fact]
    public void UnitTestProject_CanLoadBusinessLayer()
    {
        Assembly assembly = Assembly.Load("ElectronicLibrary.BLL");

        Assert.Equal("ElectronicLibrary.BLL", assembly.GetName().Name);
    }
}