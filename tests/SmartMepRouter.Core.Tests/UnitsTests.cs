using SmartMepRouter.Core.Geometry;
using Xunit;

namespace SmartMepRouter.Core.Tests
{
    public class UnitsTests
    {
        [Fact]
        public void FeetToMm_ConvertsCorrectly()
        {
            double feet = 1.0;
            double mm = Units.FeetToMm(feet);
            Assert.Equal(304.8, mm, precision: 4);
        }

        [Fact]
        public void MmToFeet_ConvertsCorrectly()
        {
            double mm = 304.8;
            double feet = Units.MmToFeet(mm);
            Assert.Equal(1.0, feet, precision: 4);
        }

        [Theory]
        [InlineData(100.0)]
        [InlineData(150.0)]
        [InlineData(0.0)]
        [InlineData(1234.56)]
        public void RoundTrip_PreservesValue(double originalMm)
        {
            double feet = Units.MmToFeet(originalMm);
            double roundTripMm = Units.FeetToMm(feet);
            Assert.Equal(originalMm, roundTripMm, precision: 6);
        }
    }
}
