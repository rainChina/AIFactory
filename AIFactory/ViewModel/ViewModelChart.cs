using AIFactory.Message;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using LiveChartsCore.Drawing;
using LiveChartsCore.SkiaSharpView.Drawing.Geometries;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView.Painting;
using LiveChartsCore.SkiaSharpView.Painting.Effects;
using LiveChartsCore.SkiaSharpView.VisualElements;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LiveChartsCore.Defaults;
using LiveChartsCore.Kernel.Sketches;

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
        public ObservableCollection<double> PressureDiffData { get; set; } = new ObservableCollection<double>() { 210, 400, 300, 350, 219, 323, 618 };

        public ObservableCollection<double> RealtimeProcessingData { get; set; } = new ObservableCollection<double>() { 210, 400, 300, 350, 219, 323, 618 };

        public ObservableCollection<ObservablePoint> RealtimeProcessingDataPoint { get; set; } = new ObservableCollection<ObservablePoint>()
        {
            new ObservablePoint(0, 22),
            new ObservablePoint(1, 30),
            new ObservablePoint(3, 40),
            new ObservablePoint(6, 6),
            new ObservablePoint(9, 12),
            new ObservablePoint(12, 32)
         };
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

        #region Series
        public ISeries[] predictionSeries { get; set; }
        public ISeries[] GassRationSeries { get; set; }

        public ISeries[] TPDifferenceSeries { get; set; }
        public ISeries[] CarbonReductionSeries { get; set; }

        private void InitialChart()
        {

            var strokeThickness = 10;
            var strokeDashArray = new float[] { 3 * strokeThickness, 2 * strokeThickness };
            var effect = new DashEffect(strokeDashArray);

            predictionSeries = new ISeries[] {
                new LineSeries<double>
                    {
                        Values = PredictionData,
                        LineSmoothness = 1,
                        GeometrySize = 22,
                        Stroke = new SolidColorPaint
                        {
                            Color = SKColors.CornflowerBlue,
                            StrokeCap = SKStrokeCap.Round,
                            StrokeThickness = strokeThickness,
                            PathEffect = effect
                        },
                        Fill = null
                    }
            };


            GassRationSeries = new ISeries[] {
                new LineSeries<double>
                {
                    Values = GasCoData,
                    Fill = null,
                    GeometrySize = 20
                },

                // use the second generic parameter to define the geometry to draw
                // there are many predefined geometries in the LiveChartsCore.Drawing namespace
                // for example, the StarGeometry, CrossGeometry, RectangleGeometry and DiamondGeometry
                new LineSeries<double, StarGeometry>
                {
                    Values = GasCo2Data,
                    Fill = null,
                    GeometrySize = 20
                },

                // You can also use SVG paths to draw the geometry
                // the VariableSVGPathGeometry can change the drawn path at runtime
                new LineSeries<double, VariableSVGPathGeometry>
                {
                    Values = GasN2Data,
                    Fill = null,
                    GeometrySvg = SVGPoints.Pin,
                    GeometrySize = 20
                },

                // finally you can also use SkiaSharp to draw your own geometry
                new LineSeries<double,CircleGeometry>
                {
                    Values = GasO2Data,
                    Fill = null,
                    GeometrySize = 20
                },
            };

            TPDifferenceSeries = new ISeries[] {
                new LineSeries<double> { Values = TemperatureData },
                new LineSeries<double> { Values = PressureDiffData } };

            CarbonReductionSeries = new ISeries[] 
            { 
                new LineSeries<ObservablePoint>
                {
                    Values =RealtimeProcessingDataPoint,
                    Fill = null,
                }
            };
        }


        public ICartesianAxis[] XAxes { get; set; } = new ICartesianAxis[] 
        {
            new Axis
            {

                CrosshairLabelsBackground = SKColors.DarkOrange.AsLvcColor(),
                CrosshairLabelsPaint = new SolidColorPaint(SKColors.DarkRed),
                CrosshairPaint = new SolidColorPaint(SKColors.DarkOrange, 1),
                Labeler = value => value.ToString("N2")
            }
        };

        public ICartesianAxis[] YAxes { get; set; } = new ICartesianAxis[] {
            new Axis
        {
            CrosshairLabelsBackground = SKColors.DarkOrange.AsLvcColor(),
            CrosshairLabelsPaint = new SolidColorPaint(SKColors.DarkRed),
            CrosshairPaint = new SolidColorPaint(SKColors.DarkOrange, 1),
            CrosshairSnapEnabled = true // snapping is also supported
        }
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

            InitialChart();
        }

    }
}
