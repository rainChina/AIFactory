using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AIFactory.Model
{
    public enum DataPointType
    {
        Gas_CO,
        Gas_CO2,
        Gas_N2,
        Gas_O2,
        Diff_Temperature,
        Diff_Pressure,
        CarbonReduction,
        RealPrediction
    }

    public class DataPoint
    {
        public DataPointType DataPointType;

        public DateTime TimeLabel;

        public double DataValue;

    }
}
