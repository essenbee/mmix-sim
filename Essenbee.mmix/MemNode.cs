using System;
using System.Diagnostics;

namespace Essenbee.mmix
{
    [DebuggerDisplay("Location = {Location.ToString(\"X16\")}, Timestamp = {Timestamp}")]
    public class MemNode
    {
        public ulong Location { get; set; } // Location of first 512 tetra-bytes (key)
        public uint Timestamp { get; set; }  // Used to ensure a balanced tree
        public MemNode Right { get; set; }
        public MemNode Left { get; set; }
        public MemTetra[] Memory { get; set; } = new MemTetra[512];

        public MemNode(ulong loc, ref uint priority)
        {
            Location = loc;
            Timestamp = priority;
            priority += 0x9E3779B9;

            for (int i = 0; i < 512; i++)
            {
                Memory[i] = new MemTetra();
            }
        }

        public static MemNode InsertMemNode(MemNode child, MemNode root)
        {
            if (root is null)
            {
                return child;
            }

            var result = child.Location.CompareTo(root.Location);

            if (result < 0)
            {
                root.Left = InsertMemNode(child, root.Left);
                if (root.Left.Timestamp < root.Timestamp)
                {
                    root = root.RotateRight();
                }
            }
            else if (result > 0)
            {
                root.Right = InsertMemNode(child, root.Right);
                if (root.Right.Timestamp < root.Timestamp)
                {
                    root = root.RotateLeft();
                }
            }

            return root;
        }

        public MemNode RotateLeft()
        {
            MemNode temp = Right;
            Right = Right.Left;
            temp.Left = this;

            return temp;
        }

        public MemNode RotateRight()
        {
            MemNode temp = Left;
            Left = Left.Right;
            temp.Right = this;

            return temp;
        }
    }
}
