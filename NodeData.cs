using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TPY
{
    public class NodeData
    {
        public string VariableName { get; set; }
        public string VariableType { get; set; }
        public double Offset { get; set; }
        public string Parent { get; internal set; }
        public int Num { get;  set; }

        public NodeData(string name, string type, double bitOffs, string parent,int num)
        {
            this.VariableName = name;
            this.VariableType = type;
            this.Offset = bitOffs;
            this.Parent = parent;
            this.Num = num;
        }
    }
    public class MemoryAddress
    {
        public double Address { get; set; }
        public int BitSize { get; set; }
        public string Tag { get; set; }

        public MemoryAddress(double address) : this(address, 0, null)
        {
        }

        public MemoryAddress(double address,int bitSize, string tag)
        {
            Address = address;
            BitSize = bitSize;
            Tag = tag;
        }

        public override string ToString()
        {
            return string.Format("0x{0:X} ({1})", Address, Tag);
        }
    }




}
