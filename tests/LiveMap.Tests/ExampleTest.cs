using Xunit;

namespace LiveMap.Tests
{
    public class ExampleTest
    {
        [Fact]
        public void BasicMath_ShouldBeCorrect()
        {
            Assert.Equal(2, 1 + 1);
        }

        [Fact]
        public void Truthy_ShouldBeTrue()
        {
            Assert.True(true);
        }
    }
}
