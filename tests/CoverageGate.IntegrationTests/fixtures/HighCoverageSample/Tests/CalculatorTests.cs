using Sample;

namespace HighCoverageSample.Tests;

public class CalculatorTests
{
    private readonly Calculator _sut = new();

    [Fact]
    public void Add_Returns_The_Sum() => Assert.Equal(5, _sut.Add(2, 3));

    [Fact]
    public void Subtract_Returns_The_Difference() => Assert.Equal(1, _sut.Subtract(3, 2));

    [Fact]
    public void Multiply_Returns_The_Product() => Assert.Equal(6, _sut.Multiply(2, 3));

    [Fact]
    public void Divide_Returns_The_Quotient() => Assert.Equal(2, _sut.Divide(6, 3));
}
