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

		private int _reconnectionInterval =  5; //seconds

        public int ReconnectionInterval
        {
			get { return _reconnectionInterval; }
			set { _reconnectionInterval = value; }
		}


		private int _dataRefreshInterval = 1;

		public int DataRefreshInterval
		{
			get { return _dataRefreshInterval; }
			set { _dataRefreshInterval = value; }
		}


		private int _predictionInterval = 1;

		public int PredictionInterval
		{
			get { return _predictionInterval; }
			set { _predictionInterval = value; }
		}



	}
}
