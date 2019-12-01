using System;
using Xunit;

namespace Essenbee.mmix.Tests
{
    public class MemorySimulationShould
    {
        [Fact]
        private void InsertMemNodesIntoTreeAndRebalances()
        {
            var sut = new mmixcpu();
            _ = sut.FindMemory(0x20000000_00000000); // a

            //`              r
            //`             /
            //`            a

            Assert.Equal<ulong>(0x20000000_00000000, sut.MemoryRoot.Left.Location); // a

            _ = sut.FindMemory(0x30000000_00000000); // b

            //`              r
            //`             /
            //`            b
            //`           /
            //`          a

            Assert.Equal<ulong>(0x30000000_00000000, sut.MemoryRoot.Left.Location);  // b
            Assert.Equal<ulong>(0x20000000_00000000, sut.MemoryRoot.Left.Left.Location); // a

            _ = sut.FindMemory(0x50000000_00000000); // c

            //`              r
            //`             / \
            //`            b   c
            //`           /
            //`          a

            Assert.Equal<ulong>(0x50000000_00000000, sut.MemoryRoot.Right.Location); // c
            Assert.Equal<ulong>(0x30000000_00000000, sut.MemoryRoot.Left.Location); // b
            Assert.Equal<ulong>(0x20000000_00000000, sut.MemoryRoot.Left.Left.Location); // a

            _ = sut.FindMemory(0x30000000_10000000); //d

            //`              r
            //`            / \
            //`            b   c
            //`           / \
            //`          a   d

            Assert.Equal<ulong>(0x50000000_00000000, sut.MemoryRoot.Right.Location); // c
            Assert.Equal<ulong>(0x30000000_00000000, sut.MemoryRoot.Left.Location);  // b
            Assert.Equal<ulong>(0x30000000_10000000, sut.MemoryRoot.Left.Right.Location); // d
            Assert.Equal<ulong>(0x20000000_00000000, sut.MemoryRoot.Left.Left.Location); // a

            _ = sut.FindMemory(0x40000000_10000000); // e

            //`              r
            //`             / \
            //`            b   e
            //`           / \   \
            //`          a   d   c

            Assert.Equal<ulong>(0x40000000_10000000, sut.MemoryRoot.Right.Location); // e
            Assert.Equal<ulong>(0x50000000_00000000, sut.MemoryRoot.Right.Right.Location); // c
            Assert.Equal<ulong>(0x30000000_00000000, sut.MemoryRoot.Left.Location); // b
            Assert.Equal<ulong>(0x30000000_10000000, sut.MemoryRoot.Left.Right.Location); // d
            Assert.Equal<ulong>(0x20000000_00000000, sut.MemoryRoot.Left.Left.Location); // a

            _ = sut.FindMemory(0x30000000_20000000); // f

            //`              r
            //`             / \
            //`            b   e
            //`           / \   \
            //`          a   d   c
            //`               \
            //`                f

            Assert.Equal<ulong>(0x40000000_10000000, sut.MemoryRoot.Right.Location); // e
            Assert.Equal<ulong>(0x50000000_00000000, sut.MemoryRoot.Right.Right.Location); // c
            Assert.Equal<ulong>(0x30000000_00000000, sut.MemoryRoot.Left.Location); // b
            Assert.Equal<ulong>(0x30000000_10000000, sut.MemoryRoot.Left.Right.Location); // d
            Assert.Equal<ulong>(0x30000000_20000000, sut.MemoryRoot.Left.Right.Right.Location); // f
            Assert.Equal<ulong>(0x20000000_00000000, sut.MemoryRoot.Left.Left.Location); //a
        }
    }
}
