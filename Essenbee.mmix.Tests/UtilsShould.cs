using Xunit;

namespace Essenbee.mmix.Tests
{
    public class UtilsShould
    {
        [Theory]
        [InlineData(0x20010203, new byte[] { 0x20, 0x01, 0x02, 0x03 })]
        [InlineData(0x00AABBCC, new byte[] { 0x00, 0xAA, 0xBB, 0xCC })]
        public void ReturnBytesFromTetra(uint tetra, byte[] expected)
        {
            var (op, x, y, z) = Utils.GetBytesFromTetra(tetra);

            Assert.Equal(expected[0], op);
            Assert.Equal(expected[1], x);
            Assert.Equal(expected[2], y);
            Assert.Equal(expected[3], z);
        }
    }
}
