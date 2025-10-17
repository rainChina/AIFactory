using AIFactory.Message;
using CommunityToolkit.Mvvm.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace AIFactory.View
{
    /// <summary>
    /// Interaction logic for WinStartup.xaml
    /// </summary>
    public partial class WinStartup
    {
        public WinStartup()
        {
            InitializeComponent();

            WeakReferenceMessenger.Default.Register<StartWindowConfirmMessage>(this, (r, m) =>
            {
                Confirm(m);
            });
        }

        private void Confirm(StartWindowConfirmMessage message)
        {
            if(message.Value)
                this.DialogResult = true;
            this.Close();
        }

    }
}
