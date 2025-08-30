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
        public ViewModelMainWindow()
        {
            IncrementCounterCommand = new RelayCommand(OnPLCItemWriteClick);
            DigitalScreenShowCommand = new RelayCommand(OnDigitalScreenShowClick);

        }

        public ICommand IncrementCounterCommand { get; }


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
