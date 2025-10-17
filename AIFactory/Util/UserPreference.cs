using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AIFactory.Util
{
    public class UserPreference : ObservableObject
    {
		public static UserPreference Instance { get; } = new UserPreference();

		private  string _addressPlc= "192.168.0.1";

		public string AddressPlc
        {
			get { return _addressPlc; }
			set { _addressPlc = value; }
		}

		private int _portPlc = 4840;

		public int PortPlc
		{
			get { return _portPlc; }
			set { _portPlc = value; }
		}


		private string _addressMES = "";

        public string AddressMES
        {
            get { return _addressMES; }
            set { _addressMES = value; }
        }

        private int _reconnectionInterval =  5; //seconds

        public int ReconnectionInterval
        {
			get { return _reconnectionInterval; }
			set { _reconnectionInterval = value; }
		}


		private int _plcReadInterval = 1;

		public int PlcReadInterval
		{
			get { return _plcReadInterval; }
			set { _plcReadInterval = value; }
		}


		private int _predictionInterval = 1;

		public int PredictionInterval
		{
			get { return _predictionInterval; }
			set { _predictionInterval = value; }
		}


		private int _mesSaveInterval = 1;

		public int MesSaveInterval
		{
			get { return _mesSaveInterval; }
			set { _mesSaveInterval = value; }
		}

	}
}
