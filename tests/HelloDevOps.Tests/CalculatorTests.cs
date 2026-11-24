using HelloDevOps.Api;
using Xunit;

namespace HelloDevOps.Tests;

public class CalculatorTests
{
    [Fact]
    public void Add_TwoNumbers_ReturnsSum()
    {
        Assert.Equal(5, Calculator.Add(2, 3));
    }

    [Theory]
    [InlineData(10, 4, 6)]
    [InlineData(0, 5, -5)]
    [InlineData(-3, -3, 0)]
    public void Subtract_ReturnsDifference(int a, int b, int expected)
    {
        Assert.Equal(expected, Calculator.Subtract(a, b));
    }
}
