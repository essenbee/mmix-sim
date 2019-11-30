using System;

namespace Essenbee.Z80
{
    public partial class mmixcpu
    {
        private byte UNDEF(byte opCode)
        {
            throw new NotImplementedException("Not yet coded this instruction");
        }
    }
}
