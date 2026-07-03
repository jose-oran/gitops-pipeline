using Sample;

namespace LowCoverageSample.Tests;

// Deliberately under-tested: only 1 of Calculator's 4 methods is covered, so line coverage
// lands well under 80% - this fixture exists to prove the threshold gate actually fails.
public class CalculatorTests
{
    private readonly Calculator _sut = new();

    [Fact]
    public void Add_Returns_The_Sum() => Assert.Equal(5, _sut.Add(2, 3));
}
