using Xunit;
using livemap.util;

namespace LiveMap.Tests
{
    public class MathfTest
    {
        [Fact]
        public void AsLong_And_Back_ShouldPreserveValues()
        {
            int x = 123456789;
            int z = -987654321;

            long index = Mathf.AsLong(x, z);

            Assert.Equal(x, Mathf.LongToX(index));
            Assert.Equal(z, Mathf.LongToZ(index));
        }

        [Fact]
        public void BlockIndex_2D_ShouldBeConsistent()
        {
            // BlockIndex(x, z) => ((z & 31) << 5) + (x & 31)
            // (1 & 31) << 5 = 32. (2 & 31) = 2. Total 34.
            Assert.Equal(34, Mathf.BlockIndex(2, 1));
        }

        [Fact]
        public void BlockIndex_3D_ShouldBeConsistent()
        {
            // BlockIndex(x, y, z) => ((((y & 31) << 5) + (z & 31)) << 5) + (x & 31)
            // x=1, y=2, z=3
            // (2 << 5) = 64. 64 + 3 = 67. 67 << 5 = 2144. 2144 + 1 = 2145.
            Assert.Equal(2145, Mathf.BlockIndex(1, 2, 3));
        }

        [Fact]
        public void Lerp_ShouldInterpolateCorrectly()
        {
            // 0 -> 10, t=0.5 => 5
            Assert.Equal(5f, Mathf.Lerp(0f, 10f, 0.5f));
            // 0 -> 10, t=0.1 => 1
            Assert.Equal(1f, Mathf.Lerp(0f, 10f, 0.1f));
        }

        [Fact]
        public void Max_ShouldReturnLargest()
        {
            Assert.Equal(10u, Mathf.Max(1u, 5u, 10u, 2u));
        }

        [Fact]
        public void Min_ShouldReturnSmallest()
        {
            Assert.Equal(1u, Mathf.Min(1u, 5u, 10u, 2u));
        }
    }
}
