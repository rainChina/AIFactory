using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore;
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
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SQLiteViewer
{
    /// <summary>
    /// Interaction logic for WinDataViewer.xaml
    /// </summary>
    public partial class WinDataViewer : Window, INotifyPropertyChanged
    {

        // This property should be bound to your Chart in XAML
        private ISeries[] _series;
        private Axis[] _xAxes;

        public ISeries[] Series
        {
            get => _series;
            set
            {
                _series = value;
                OnPropertyChanged();
            }
        }

        public Axis[] XAxes
        {
            get => _xAxes;
            set
            {
                _xAxes = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public WinDataViewer()
        {
            InitializeComponent();
            lvcChart.DataContext = this;

        }


        public async Task LoadChartAsync(List<SensorGroup> groupedResults)
        {
            if (groupedResults == null || !groupedResults.Any()) return;

            // 1. Move the heavy mapping and normalization to a background thread
            var result = await Task.Run(() =>
            {
                var seriesList = new List<ISeries>();

                foreach (var group in groupedResults)
                {
                    if (group.ChartPoints == null || !group.ChartPoints.Any()) continue;

                    // Find min and max for this specific group to normalize
                    double min = group.ChartPoints.Min(p => p.Value);
                    double max = group.ChartPoints.Max(p => p.Value);
                    double range = max - min;

                    // Sorting and mapping with Normalization logic
                    var chartPoints = group.ChartPoints
                        .OrderBy(p => p.Time)
                        .Select(p =>
                        {
                            // Avoid division by zero if all values are the same
                            double normalizedValue = range > 0
                                ? (p.Value - min) / range
                                : 0;

                            return new DateTimePoint(p.Time, normalizedValue);
                        })
                        .ToList();

                    seriesList.Add(new LineSeries<DateTimePoint>
                    {
                        Name = group.NameID,
                        Values = chartPoints,
                        Fill = null,
                        GeometrySize = 0,
                        LineSmoothness = 0
                    });
                }

                var xAxes = new Axis[]
                {
            new Axis
            {
                Labeler = value => value <= 0 ? "" : new DateTime((long)value).ToString("HH:mm:ss"),
                LabelsRotation = 15,
                UnitWidth = TimeSpan.FromSeconds(1).Ticks,
                MinStep = TimeSpan.FromSeconds(1).Ticks
            }
                };

                return new { Series = seriesList.ToArray(), Axes = xAxes };
            });

            // 2. Return to the UI thread to update the bound properties
            this.Series = result.Series;
            this.XAxes = result.Axes;
        }
    }
}
