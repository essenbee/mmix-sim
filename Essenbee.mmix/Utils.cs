using System;
using System.Net;

namespace Essenbee.mmix
{
    public static class Utils
    {
        public static (byte op, byte x, byte y, byte z) GetBytesFromTetra(uint tetra)
        {
            byte[] b = BitConverter.GetBytes(tetra);

            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(b);
            }

            return (b[0], b[1], b[2], b[3]);
        }
    }
}
