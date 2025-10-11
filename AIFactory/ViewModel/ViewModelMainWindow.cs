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

            StartTasks();
            //StartPLCTask();
            //StartPredictTask();
        }

        //private CancellationTokenSource _cancellationTokenSource;

        //private void StartPLCTask()
        //{
        //    _cancellationTokenSource = new CancellationTokenSource();
        //    var token = _cancellationTokenSource.Token;

        //    Task.Run(async () =>
        //    {
        //        while (!token.IsCancellationRequested)
        //        {
        //            // Simulate reading data
        //            string data = await ReadDataAsync();

        //            // Update UI safely
        //            Dispatcher.Invoke(() =>
        //            {
        //                StatusText.Text = $"Data: {data} at {DateTime.Now:T}";
        //            });

        //            await Task.Delay(2000, token); // Wait 2 seconds
        //        }
        //    }, token);
        //}

        public void StartPLCTask()
        {
            plcOpc = new PLCOPCManager(OPCIP);

            int _rectryInterval = userPreference.ReconnectionInterval * 1000;
            int _dataRefreshInterval = userPreference.DataRefreshInterval * 1000;
            var task = Task.Run(async () =>
            {
                bool blConnected = false;
                while (!blConnected)
                {
                    blConnected = await plcOpc.Connect();
                    if (blConnected == false)
                    {
                        await Task.Delay(_rectryInterval); // 等待5秒后重试
                    }
                }

                if(blConnected)
                {
                    DispatcherNofication("PLC连接成功", "系统信息");
                }

                while (true)
                {
                    if (blConnected)
                    {
                        break;
                    }

                    ReadPLCData();

                    await Task.Delay(_dataRefreshInterval);

                }
            }
            );
        }

        public void ReadPLCData()
        {

            foreach (var n in plcNodes)
            {
                if (n.NodeId == null)
                {
                    continue;
                }

                var res = plcOpc.ReadDatabyNodeID(n.NodeId);
                if (res != null)
                {
                    //n.NodeName
                    res.DataPointType = n.DataType;
                    if(n.DataType == DataPointType.Gas_CO)
                    {
                        //_dataQueue.Add(res.DataValue);
                    }

                    WeakReferenceMessenger.Default.Send(new GasMessage(res));
                }

            }

        }

        List<PLCNode> plcNodes = new List<PLCNode>();
        private void InitalNodeInfo()
        {
            plcNodes.Add(new PLCNode() { DataType = DataPointType.Gas_CO, NodeId = "ns=2;s=Channel1.Device1.Tag1", NodeName = "Tag1" });
            plcNodes.Add(new PLCNode() { DataType = DataPointType.Gas_CO2, NodeId = "ns=2;s=Channel1.Device1.Tag2", NodeName = "Tag2" });
            plcNodes.Add(new PLCNode() { DataType = DataPointType.Gas_O2, NodeId = "ns=2;s=Channel1.Device1.Tag3", NodeName = "Tag3" });
            plcNodes.Add(new PLCNode() { DataType = DataPointType.Gas_N2, NodeId = "ns=2;s=Channel1.Device1.Tag4", NodeName = "Tag4" });
            plcNodes.Add(new PLCNode() { DataType = DataPointType.Gas_CO, NodeId = "ns=2;s=Channel1.Device1.Tag5", NodeName = "Tag5" });
            plcNodes.Add(new PLCNode() { DataType = DataPointType.Diff_Temperature, NodeId = "ns=2;s=Channel1.Device1.Tag5", NodeName = "Tag5" });
            plcNodes.Add(new PLCNode() { DataType = DataPointType.Diff_Pressure, NodeId = "ns=2;s=Channel1.Device1.Tag5", NodeName = "Tag5" });
            plcNodes.Add(new PLCNode() { DataType = DataPointType.CarbonReduction, NodeId = "ns=2;s=Channel1.Device1.Tag5", NodeName = "Tag5" });
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

        SQLiteManager sqLiteManager;

        private BlockingCollection<DataRealTime> _dataQueue = new();
        private CancellationTokenSource cts = new CancellationTokenSource();
        private void SaveDataToSQLite(CancellationToken token)
        {
            //using (var db = new SensorContext())
            //{
            //    db.Database.EnsureCreated();

            //    while (!token.IsCancellationRequested)
            //    {
            //        var data = _dataQueue.Take(); // Blocks until data is available
            //        //var measurement = new Measurement
            //        //{
            //        //    Value = data,
            //        //    Timestamp = DateTime.Now
            //        //};
            //        db.SensorRecords.Add(data);
            //        db.SaveChanges();
            //    }
            //}

            if(sqLiteManager == null)
            {
                sqLiteManager = new SQLiteManager();
            }

            while (!token.IsCancellationRequested)
            {
                var data = _dataQueue.Take(); // Blocks until data is available
                string json = JsonSerializer.Serialize(data);
                sqLiteManager.SaveJson(json);
            }
            

        }


        int _rectryInterval = UserPreference.Instance.ReconnectionInterval * 1000;
        int _dataRefreshInterval = UserPreference.Instance.DataRefreshInterval * 1000;
        private async Task GetDataFromInstrumentAsync(CancellationToken token)
        {

            plcOpc = new PLCOPCManager(OPCIP);
            while (!token.IsCancellationRequested)
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

                while (true)
                {
                    if (blConnected)
                    {
                        break;
                    }

                   var data = plcOpc.GetRealTimeData();

                    foreach(var p in data)
                    {
                        _dataQueue.Add(p);
                    }

                    await Task.Delay(_dataRefreshInterval);

                }
            }
        }

        private void StartTasks()
        {
            // Task 1: Get data from SCPI instrument
            Task.Run(() => GetDataFromInstrumentAsync(cts.Token));

            // Task 2: Save data to SQLite using EF Core
            Task.Run(() => SaveDataToSQLite(cts.Token));
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
                    var item = _dataQueue.Take(); // Waits until an item is available

                    //inputDataList.Add(item);
                    if(inputDataList.Count < LstmInputCount)
                    {
                        continue;
                    }

                    // Simulate reading data
                    double[] inputData = inputDataList.ToArray();
                    string prediction = PythonCaller.CallPythonLSTM(inputData);
                    Console.WriteLine($"Predicted value: {prediction}");

                    DataPoint res = new DataPoint();
                    res.DataValue = double.Parse(prediction);
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
                sqLiteManager?.Close();
            }

            // Free any unmanaged resources here.

            disposed = true;
        }
    }
}
