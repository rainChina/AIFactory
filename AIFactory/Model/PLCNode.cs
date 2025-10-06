using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AIFactory.Model
{
    class PLCNode
    {
        public DataPointType DataType;
        public string? NodeId { get; set; }
        public string? NodeName { get; set; }

    }

    [Serializable]
    public class NodeAttribute
    {
        public string? NodeId { get; set; }
        public string? NodeName { get; set; }
        public string? NodeDeisplayName { get; set; }
        public string? NodeDescription { get; set; }
        public string? NodeDataType { get; set; }
    }

}
