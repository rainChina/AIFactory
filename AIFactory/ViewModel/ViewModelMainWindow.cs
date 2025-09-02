using AIFactory.Util;
using AIFactory.View;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
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

           plcOpc = new PLCOPCManager(OPCIP);

        }

        public void Start()
        {
            var task = Task.Run(async () => await plcOpc.Connect());
            task.Wait();

            if(task.Result == true)
            {
                ReadPLCItems();
            }
        }   

        public void ReadPLCItems()
        {
            var task = Task.Run(async () => await plcOpc.ReadPLCItems());
            task.Wait();
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
