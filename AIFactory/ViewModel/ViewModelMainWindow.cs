using AIFactory.Message;
using AIFactory.Model;
using AIFactory.Util;
using AIFactory.View;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

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


        public ViewModelMainWindow()
        {
            IncrementCounterCommand = new RelayCommand(OnPLCItemWriteClick);
            DigitalScreenShowCommand = new RelayCommand(OnDigitalScreenShowClick);

            InitalNodeInfo();

            StartPLC();
        }

        public void StartPLC()
        {
            plcOpc = new PLCOPCManager(OPCIP);

            var task = Task.Run(async () => await plcOpc.Connect());
            task.Wait();

            if (task.Result == true)
            {
                StartChartRefresh();
            }
        }
        System.Timers.Timer timerRefreshData;
        private void StartChartRefresh()
        {
            timerRefreshData = new System.Timers.Timer(1000); // 设置定时器间隔为1秒
            timerRefreshData.Elapsed += (sender, e) =>
            {
                ReadPLCItems();
            };
            timerRefreshData.AutoReset = true; // 设置为true表示定时器会重复触发
            timerRefreshData.Enabled = true; // 启动定时器
        }

        public void ReadPLCItems()
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


                    double[] inputData = new double[] { 0.1, 0.2, 0.3, 0.4, 0.5, 0.6, 0.7, 0.8, 0.9, 1.0 };
                    string prediction = PythonCaller.CallPythonLSTM(inputData);
                    Console.WriteLine($"Predicted value: {prediction}");



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


    }
}
