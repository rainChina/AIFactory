using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AIFactory.Model
{
    public class SensorData
    {
        public int Id { get; set; }
        public DateTime Timestamp { get; set; }
        public double Temperature { get; set; }
        public string Status { get; set; }
    }


    public class DataRealTime
    {
        public string NameID { get; set; }

        public float Value { get; set; }

        public DateTime TimeRefresh { get; set; }

        public DateTime TimeRead { get; set; }
    }

}
