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

namespace SQLiteViewer;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
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


    private void LoadData(string dbPath)
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

            //var groupedResults = items
            //    .Select(row =>
            //    {
            //        string jsonColumnContent = row.Json;
            //        if (string.IsNullOrEmpty(jsonColumnContent)) return null;
            //        return JsonNode.Parse(jsonColumnContent);
            //    })
            //    .Where(inner => inner != null)
            //    .GroupBy(inner => inner["NameID"]?.ToString())
            //    .Select(group => new
            //    {
            //        NameID = group.Key,
            //        // We create a collection of DateTimePoints for LiveCharts
            //        ChartPoints = group.Select(node => new
            //        {
            //            Time = DateTime.Parse(node["TimeRefresh"]?.ToString() ?? DateTime.Now.ToString()),
            //            Value = ConvertToDouble(ExtractSmartValue(node["Value"]))
            //        })
            //        .OrderBy(p => p.Time) // Ensure chronological order for the line
            //        .ToList()
            //    })
            //    .ToList();

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

            LoadChart(groupedResults);

            WinChart charWin = new WinChart(groupedResults);
            charWin.Show();

        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading data: {ex.Message}");
        }
    }

    // This property should be bound to your Chart in XAML
    public ISeries[] Series { get; set; }
    public Axis[] XAxes { get; set; }

    private void LoadChart(List<SensorGroup> groupedResults)
    {
        var seriesList = new List<ISeries>();

        //foreach (var group in groupedResults)
        //{
        //    seriesList.Add(new LineSeries<DateTimePoint>
        //    {
        //        Name = group.NameID,
        //        Values = group.ChartPoints.Select(p => new DateTimePoint(p.Time, p.Value)).ToArray(),
        //        Fill = null, // Removes the area fill under the line
        //        GeometrySize = 5 // Size of the dots on the line
        //    });
        //}

        //foreach (var group in groupedResults)
        //{
        //    // 1. Cast the dynamic ChartPoints to a typed List
        //    var points = (IEnumerable<MyDataPoint>)group.ChartPoints;

        //    seriesList.Add(new LineSeries<DateTimePoint>
        //    {
        //        Name = group.NameID,
        //        // 2. Now the compiler knows exactly what 'p' is
        //        Values = points.Select(p => new DateTimePoint(p.Time, p.Value)).ToList(),

        //        Fill = null,
        //        GeometrySize = 5
        //    });
        //}
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