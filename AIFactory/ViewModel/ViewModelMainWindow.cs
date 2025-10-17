using AIFactory.Message;
using AIFactory.Model;
using AIFactory.Util;
using AIFactory.View;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace AIFactory.ViewModel
{
    public partial class ViewModelMainWindow : ObservableObject
    {
        private string _opcIP;

        public string OPCIP
        {
            get { return _opcIP; }
            set { _opcIP = value; }
        }


        private UserPreference userPreference;
        public ViewModelMainWindow()
        {

            userPreference = App.Services.GetService<UserPreference>();
            IncrementCounterCommand = new RelayCommand(OnPLCItemWriteClick);
            DigitalScreenShowCommand = new RelayCommand(OnDigitalScreenShowClick);

            //StartTasks();
            //StartPredictTask();
        }
        public ICommand IncrementCounterCommand { get; }

        PLCOPCManager plcOpc;

        [RelayCommand]
        public void OnPLCItemWriteClick()
        {
            var plcWriter = new WinPLCWriter();

            plcWriter.Show();
        }

        public ICommand DigitalScreenShowCommand { get; }


        [RelayCommand]
        public void OnDigitalScreenShowClick()
        {
            var plcWriter = new WinDigitalScreen();

            plcWriter.Show();
        }

        SQLiteManager plcSqLiteManager;
        SQLiteManager mesSqLiteManager;

        private readonly BlockingCollection<DataRealTime> _dataQueuePLC = new();
        private CancellationTokenSource cts = new CancellationTokenSource();
        private void SavePLCDataToSQLite(CancellationToken token)
        {
            if (plcSqLiteManager == null)
            {
                plcSqLiteManager = new SQLiteManager("DataPLC");
            }
            bool blDataSaved = false;

            while (!token.IsCancellationRequested)
            {
                var data = _dataQueuePLC.Take(); // Blocks until data is available
                string json = JsonSerializer.Serialize(data);
                plcSqLiteManager.SaveJson(json);
                Task.Delay(_mesDataSaveInterval);
                if (!blDataSaved)
                {
                    DispatcherNofication("PLC数据保存中...", "系统信息");
                    blDataSaved = true;
                }
            }


        }


        int _rectryInterval = UserPreference.Instance.ReconnectionInterval * 1000;
        int _dataRefreshInterval = UserPreference.Instance.DataRefreshInterval * 1000;

        int _mesDataSaveInterval = UserPreference.Instance.MesSaveInterval * 1000;

        private async Task GetDataFromInstrumentAsync(CancellationToken token)
        {
            DispatcherNofication("PLC连接中...", "系统信息");

            Dispatcher.CurrentDispatcher.Invoke(() =>
            {
                ClearChartDataMessage msg = new ClearChartDataMessage("Data Chart Init..");
                WeakReferenceMessenger.Default.Send(msg);

            });
            plcOpc = new PLCOPCManager(OPCIP);
            //while (!token.IsCancellationRequested)
            {
                bool blConnected = false;
                while (!token.IsCancellationRequested && !blConnected)
                {
                    blConnected = await plcOpc.Connect();
                    if (blConnected == false)
                    {
                        await Task.Delay(_rectryInterval); // 等待5秒后重试
                    }
                }

                if (blConnected)
                {
                    DispatcherNofication("PLC连接成功", "系统信息");
                }

                while (!token.IsCancellationRequested)
                {
                    var data = plcOpc.GetRealTimeData();

                    foreach (var p in data)
                    {
                        _dataQueuePLC.Add(p);

                        DispatchChartData(p);


                    }

                    await Task.Delay(_dataRefreshInterval);

                }
            }
        }

        private void DispatchChartData(DataRealTime dataPoint)
        {
            //Dispatcher.CurrentDispatcher.Invoke(() =>
            {
                if (!ChartDatamapping.Keys.Contains(dataPoint.NameID))
                {
                    return;
                }
                DataPoint res = new DataPoint();

                res.DataValue = dataPoint.Value;
                res.DataPointType = ChartDatamapping[dataPoint.NameID];
                res.TimeLabel = dataPoint.TimeRefresh;
                WeakReferenceMessenger.Default.Send(new GasMessage(res));
            };
        }



        private string mesServer = "https://testapi.jasonwatmore.com/products/1";
        private MESClient _mesClient;
        private readonly BlockingCollection<string> _mesBlockCollection = new();
        SQLiteManager mesSqlit;
        private async Task GetMesDataAsync(CancellationToken token)
        {
            if (_mesClient == null)
                _mesClient = new MESClient(mesServer);
            DispatcherNofication("MES连接中...", "系统信息");
            bool blMSConneced = false;
            while (!token.IsCancellationRequested)
            {
                var data = await _mesClient.FetchDataAsync();
                if (data != null)
                {
                    _mesBlockCollection.Add(data);
                    if (!blMSConneced)
                    {
                        blMSConneced = true;
                        DispatcherNofication("MES连接成功！", "系统信息");

                    }
                }
                await Task.Delay(_mesDataSaveInterval);
            }

        }
        private void SaveMesData(CancellationToken token)
        {
            if (mesSqlit == null)
            {
                mesSqlit = new SQLiteManager("DataMES");
            }
            bool blMESSaved = false;
            while (!token.IsCancellationRequested)
            {
                try
                {
                    if (_mesBlockCollection.Count == 0)
                    {
                        Task.Delay(_mesDataSaveInterval).Wait();
                        continue;
                    }

                    var data = _mesBlockCollection.Take(); // Blocks until data is available
                    mesSqlit.SaveJson(data);

                    if (!blMESSaved)
                    {
                        blMESSaved = true;
                        DispatcherNofication("MES结果保存中...!", "系统信息");

                    }
                    Task.Delay(_mesDataSaveInterval);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error checking collection count: {ex.Message}");
                    continue;
                }

            }
        }


        private void StartTasks()
        {
            //// Task 1: Get data from SCPI instrument
            Task.Run(() => GetDataFromInstrumentAsync(cts.Token));

            //// Task 2: Save data to SQLite using EF Core
            Task.Run(() => SavePLCDataToSQLite(cts.Token));

            // Task 3: Fetch data from MES server
            Task.Run(() => GetMesDataAsync(cts.Token));

            // Task 4: Save MES data to SQLite
            Task.Run(() => SaveMesData(cts.Token));

        }


        List<double> inputDataList = new List<double>();
        int LstmInputCount = 10;
        private void StartPredictTask()
        {
            int _dataRefreshInterval = userPreference.PredictionInterval * 1000;

            var task = Task.Run(async () =>
            {
                while (true)
                {
                    var item = _dataQueuePLC.Take(); // Waits until an item is available

                    //inputDataList.Add(item);
                    if (inputDataList.Count < LstmInputCount)
                    {
                        continue;
                    }

                    // Simulate reading data
                    double[] inputData = inputDataList.ToArray();
                    string prediction = PythonCaller.CallPythonLSTM(inputData);
                    Console.WriteLine($"Predicted value: {prediction}");

                    DataPoint res = new DataPoint();
                    //res.DataValue = double.Parse(prediction);
                    res.DataPointType = DataPointType.RealPrediction;
                    GasMessage gasMessage = new GasMessage(res);

                    WeakReferenceMessenger.Default.Send(new GasMessage(res));

                    await Task.Delay(_dataRefreshInterval); // Wait 5 seconds
                }
            }
            );
        }

        private void DispatcherNofication(string message, string v)
        {
            Dispatcher.CurrentDispatcher.Invoke(() =>

            {
                HandyControl.Controls.Growl.Info(message);
            });

        }

        // Flag to detect redundant calls
        private bool disposed = false;
        // Protected implementation of Dispose pattern
        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
                return;

            if (disposing)
            {
                // Free any managed objects here.
                // For example: if you had a stream or database connection, dispose it here.
                cts.Cancel();
                plcOpc?.Close();
                plcSqLiteManager?.Close();
            }

            // Free any unmanaged resources here.

            disposed = true;
        }



        private Dictionary<string, DataPointType> _chartDatamapping;

        public Dictionary<string, DataPointType> ChartDatamapping
        {
            get
            {
                if (_chartDatamapping == null)
                    _chartDatamapping = InitChartDataMapping();
                return _chartDatamapping;
            }
        }

        public Dictionary<string, DataPointType> InitChartDataMapping()
        {
            Dictionary<string, DataPointType> chartDataMapping = new Dictionary<string, DataPointType>
        {
            { "CO_Concentration_Position1", DataPointType.Gas_CO },
            { "CO2_Concentration_Position1", DataPointType.Gas_CO2 },
            { "N2_Concentration_Position3", DataPointType.Gas_N2 },
            { "O2_Concentration_Position1", DataPointType.Gas_O2 },
            { "Diff_Temperature", DataPointType.Diff_Temperature },
            { "Diff_Pressure", DataPointType.Diff_Pressure },
            { "CarbonReduction", DataPointType.CarbonReduction },
            { "RealPrediction", DataPointType.RealPrediction }
        };
            return chartDataMapping;
        }
    }
}
