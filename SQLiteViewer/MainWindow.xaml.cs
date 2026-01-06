using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore;
using Microsoft.Data.Sqlite;
using Microsoft.Win32;
using System.Text;
using System.Text.Json.Nodes;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace SQLiteViewer;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window, INotifyPropertyChanged
{
    public MainWindow()
    {
        InitializeComponent();
        lvcChart.DataContext = this;
    }
    private void OpenFile_Click(object sender, RoutedEventArgs e)
    {
        OpenFileDialog openFileDialog = new OpenFileDialog
        {
            Filter = "SQLite Database (*.db)|*.db|All files (*.*)|*.*"
        };

        if (openFileDialog.ShowDialog() == true)
        {
            string filePath = openFileDialog.FileName;
            LoadData(filePath);
        }
    }

    private void LoadJsonData(string dbPath)
    {
        var items = new List<dynamic>();

        try
        {
            using (var connection = new SqliteConnection($"Data Source={dbPath};"))
            {
                connection.Open();
                string query = "SELECT id, JsonData FROM RealTimeData"; // 修改为你的表名和字段名

                using (var command = new SqliteCommand(query, connection))
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {

                        items.Add(new
                        {
                            Id = reader["id"],
                            Json = reader["JsonData"]
                        });
                    }
                }
            }

            dataGrid.ItemsSource = items;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading data: {ex.Message}");
        }
    }


    private async Task LoadData(string dbPath)
    {
        var items = new List<dynamic>();

        try
        {
            using (var connection = new SqliteConnection($"Data Source={dbPath};"))
            {
                connection.Open();
                string query = "SELECT id, JsonData FROM RealTimeData"; // 修改为你的表名和字段名

                using (var command = new SqliteCommand(query, connection))
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {

                        items.Add(new
                        {
                            Id = reader["id"],
                            Json = reader["JsonData"]
                        });
                    }
                }
            }

            dataGrid.ItemsSource = items;

            // Now groupedResults is List<SensorGroup>, no more 'dynamic' headache!
            List<SensorGroup> groupedResults = items
                .Select(row => JsonNode.Parse((string)row.Json))
                .GroupBy(node => node["NameID"]?.ToString())
                .Select(group => new SensorGroup
                {
                    NameID = group.Key ?? "Unknown",
                    ChartPoints = group.Select(node => new SensorDataPoint
                    {
                        Time = DateTime.Parse(node["TimeRefresh"]?.ToString() ?? DateTime.Now.ToString()),
                        Value = ConvertToDouble(ExtractSmartValue(node["Value"]))
                    }).ToList()
                })
                .ToList();

            await LoadChartAsync(groupedResults);

            WinChart charWin = new WinChart(groupedResults);
            charWin.Show();

        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading data: {ex.Message}");
        }
    }

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

    private void LoadChart1(List<SensorGroup> groupedResults)
    {
        var seriesList = new List<ISeries>();

        // This is now perfectly type-safe
        foreach (SensorGroup group in groupedResults)
        {
            seriesList.Add(new LineSeries<DateTimePoint>
            {
                Name = group.NameID,
                Values = group.ChartPoints
                    .Select(p => new DateTimePoint(p.Time, p.Value))
                    .ToList(),
                Fill = null,
                GeometrySize = 5
            });
        }
        Series = seriesList.ToArray();

        // Configure the X-Axis to show time labels
        XAxes = new Axis[]
        {
        new Axis
        {
            Labeler = value => new DateTime((long)value).ToString("HH:mm:ss"),
            LabelsRotation = 15,
            UnitWidth = TimeSpan.FromSeconds(1).Ticks,
            MinStep = TimeSpan.FromSeconds(1).Ticks
        }
        };
    }
    private void LoadChart(List<SensorGroup> groupedResults)
    {
        if (groupedResults == null || !groupedResults.Any()) return;

        var seriesList = new List<ISeries>();

        foreach (var group in groupedResults)
        {
            // FIX 1: Ensure data is sorted by Time. 
            // LiveCharts draws points in the order they appear in the list.
            // Out-of-order data creates "spider-web" lines.
            var sortedPoints = group.ChartPoints
                .OrderBy(p => p.Time)
                .Select(p => new DateTimePoint(p.Time, p.Value))
                .ToList();

            seriesList.Add(new LineSeries<DateTimePoint>
            {
                Name = group.NameID,
                Values = sortedPoints,
                Fill = null,
                GeometrySize = 5
            });
        }

        // FIX 3: Assign to properties that trigger OnPropertyChanged()
        // Make sure your Series and XAxes properties call OnPropertyChanged in their setters!
        Series = seriesList.ToArray();

        XAxes = new Axis[]
        {
        new Axis
        {
            // FIX 4: Use the built-in DateTime format helper for safer scaling
            Labeler = value => value <= 0 ? "" : new DateTime((long)value).ToString("HH:mm:ss"),
            LabelsRotation = 15,
            
            // UnitWidth and MinStep are crucial for DateTime axes
            UnitWidth = TimeSpan.FromSeconds(1).Ticks,
            MinStep = TimeSpan.FromSeconds(1).Ticks
        }
        };
    }


    public async Task LoadChartAsync(List<SensorGroup> groupedResults)
    {
        if (groupedResults == null || !groupedResults.Any()) return;

        // 1. Move the heavy mapping to a background thread
        var result = await Task.Run(() =>
        {
            var seriesList = new List<ISeries>();

            foreach (var group in groupedResults)
            {
                // Sorting and mapping thousands of points is CPU intensive
                var chartPoints = group.ChartPoints
                    .OrderBy(p => p.Time)
                    .Select(p => new DateTimePoint(p.Time, p.Value))
                    .ToList();

                seriesList.Add(new LineSeries<DateTimePoint>
                {
                    Name = group.NameID,
                    Values = chartPoints,
                    Fill = null,
                    GeometrySize = 0, // OPTIMIZATION: GeometrySize 0 draws MUCH faster
                    LineSmoothness = 0 // OPTIMIZATION: Disabling curves saves CPU
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
        // This triggers the INotifyPropertyChanged events
        this.Series = result.Series;
        this.XAxes = result.Axes;
    }

    // Helper to ensure values are numeric for the chart
    private double ConvertToDouble(object val)
    {
        if (val is bool b) return b ? 1.0 : 0.0;
        if (val is int i) return (double)i;
        if (val is double d) return d;
        if (double.TryParse(val?.ToString(), out double parsed)) return parsed;
        return 0;
    }

    public static object ExtractSmartValue(JsonNode node)
    {
        if (node == null) return null;
        string text = node.ToString();
        if (bool.TryParse(text, out bool b)) return b;
        if (int.TryParse(text, out int i)) return i;
        if (double.TryParse(text, out double d)) return d;
        return text;
    }
}

public class MyDataPoint
{
    public DateTime Time { get; set; }
    public double Value { get; set; }
}

public class SensorDataPoint
{
    public DateTime Time { get; set; }
    public double Value { get; set; }
}

public class SensorGroup
{
    public string NameID { get; set; }
    public List<SensorDataPoint> ChartPoints { get; set; }
}