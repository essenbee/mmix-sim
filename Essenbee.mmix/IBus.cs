using System.Collections.Generic;

namespace Essenbee.Z80
{
    public interface IBus
    {
        IReadOnlyCollection<long> RAM { get; }
        byte Read(long addr, bool ro = false);
        void Write(long addr, byte data);
    }
}
