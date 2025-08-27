using AIFactory.Message;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using LiveChartsCore.SkiaSharpView.Painting;
using LiveChartsCore.SkiaSharpView.VisualElements;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AIFactory.ViewModel
{
    public class ViewModelChart : ObservableObject
    {
        #region Data
        public ObservableCollection<double> PredictionData { get; set; } = new ObservableCollection<double>() { 4, 2, 5, 2, 4, 5, 3 };
        public ObservableCollection<double> GasCoData { get; set; } = new ObservableCollection<double>() { 2, 1, 4, 2, 2, -5, -2 };
        public ObservableCollection<double> GasCo2Data { get; set; } = new ObservableCollection<double>() { 3, 3, -3, -2, -4, -3, -1 };
        public ObservableCollection<double> GasN2Data { get; set; } = new ObservableCollection<double>() { -2, 2, 1, 3, -1, 4, 3 };
        public ObservableCollection<double> GasO2Data { get; set; } = new ObservableCollection<double>() { 4, 2, 5, 2, 4, 5, 3 };

        public ObservableCollection<double> TemperatureData { get; set; } = new ObservableCollection<double>() { 4, 2, 5, 2, 4, 5, 3 };
        #endregion

        #region Titles
        public LabelVisual TitleCarbonReductionAmount { get; set; } =

            new LabelVisual
            {
                Text = "脱碳量",
                Paint = new SolidColorPaint(SKColors.White),
                TextSize = 18,
                Padding = new LiveChartsCore.Drawing.Padding(5),
            };

        public LabelVisual TitleRealtimeProgress { get; set; } =
          new LabelVisual
          {
              Text = "实时进展",
              Paint = new SolidColorPaint(SKColors.White),
              TextSize = 18,
              Padding = new LiveChartsCore.Drawing.Padding(5),
          };
        public LabelVisual TitleTemperaturePressureDifference { get; set; } =
         new LabelVisual
         {
             Text = "温度+压差",
             Paint = new SolidColorPaint(SKColors.White),
             TextSize = 18,
             Padding = new LiveChartsCore.Drawing.Padding(5),
         };
        public LabelVisual TitleGasContent { get; set; } =
         new LabelVisual
         {
             Text = "CO+CO2+N2+O2",
             Paint = new SolidColorPaint(SKColors.White),
             TextSize = 18,
             Padding = new LiveChartsCore.Drawing.Padding(5),
         };

        #endregion


        private void ClearDataCollection()
        {
            PredictionData.Clear();
            GasCoData.Clear();
            GasCo2Data.Clear();
            GasN2Data.Clear();
            GasO2Data.Clear();
        }

       
        public ViewModelChart()
        {
            WeakReferenceMessenger.Default.Register<ClearChartDataMessage>(this, (r, m) =>
            {
                ClearDataCollection();
            });
        }

    }
}
