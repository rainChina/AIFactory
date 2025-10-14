using AIFactory.Model;
using AIFactory.Util;
using HandyControl.Tools.Extension;
using Microsoft.Win32;
using Opc.Ua;
using System.Collections.ObjectModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml.Serialization;

namespace NodeIterator;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        nodeDataGrid.ItemsSource = nodeAttributes;
    }

    public ObservableCollection<NodeAttribute> nodeAttributes = new ObservableCollection<NodeAttribute>();
    private void Load_Click(object sender, RoutedEventArgs e)
    {
        OpenFileDialog openFileDialog = new OpenFileDialog();
        {
            // 设置对话框标题
            openFileDialog.Title = "选择 XML 文件";

            // 过滤文件类型（只显示 .xml 文件）
            openFileDialog.Filter = "XML 文件 (*.xml)|*.xml|所有文件 (*.*)|*.*";

            // 允许选择多个文件（可选，根据需求设置）
            openFileDialog.Multiselect = false;

            // 若用户选择了文件并点击“打开”
            if (openFileDialog.ShowDialog() is true)
            {
                string filePath = openFileDialog.FileName;
                var serializer = new XmlSerializer(typeof(List<NodeAttribute>));

                using (var stream = new FileStream(filePath, FileMode.Open))
                {
                    // 反序列化为容器类
                    var bookList = serializer.Deserialize(stream) as List<NodeAttribute>;
                    if (bookList != null)
                    {
                        nodeAttributes.Clear();
                        nodeAttributes.AddRange(bookList);
                    }
                    //nodeAttributes.AddRange(bookList);
                }
                //var nAttributes = IterateDescription();
                //nodeAttributes.Clear();
                //nodeAttributes.AddRange(nAttributes);
            }

        }
    }
    private void Save_Click(object sender, RoutedEventArgs e)
    {
        SaveFileDialog saveFileDialog = new SaveFileDialog
        {
            Filter = "XML files (*.xml)|*.xml",
            Title = "Save XML File",
            FileName = "data.xml"
        };

        if (saveFileDialog.ShowDialog() is true)
        {
            string filePath = saveFileDialog.FileName;

            var serializer = new XmlSerializer(typeof(ObservableCollection<NodeAttribute>));
            using (var writer = new StreamWriter(filePath))
            {
                serializer.Serialize(writer, nodeAttributes);
            }


            MessageBox.Show("File saved successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private void Connect_Click(object sender, RoutedEventArgs e)
    {
        var nAttributes = IterateDescription();
        nodeAttributes.Clear();
        nodeAttributes.AddRange(nAttributes);
    }

    HashSet<string> uniqueLines = new HashSet<string>();
    private List<NodeAttribute> IterateDescription()
    {
        List<NodeAttribute> nodeAttributes = new List<NodeAttribute>();

        // Create OpenFileDialog
        OpenFileDialog openFileDialog = new OpenFileDialog
        {
            Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*"
        };

        // Show dialog and check if user selected a file
        if (openFileDialog.ShowDialog() == true)
        {
            try
            {
                // Read all lines into a HashSet
                HashSet<string> lines = new HashSet<string>(File.ReadAllLines(openFileDialog.FileName));

                // Example: Display count
                MessageBox.Show($"Loaded {lines.Count} unique lines.");

                PLCOPCManager plcManager = new PLCOPCManager("");

                var nodes = plcManager.LoopNode("");
                foreach (var node in nodes)
                {
                    if (!string.IsNullOrEmpty(node.Key) && uniqueLines.Contains(node.Key))
                    {
                        nodeAttributes.Add(new NodeAttribute
                        {
                            NodeId = node.Key,
                            NodeName = node.Value.NodeName,
                            NodeDeisplayName = node.Value.NodeDeisplayName,
                            NodeDescription = node.Value.NodeDescription,
                            NodeDataType = node.Value.NodeDataType.ToString()
                        });
                    }
                }

                plcManager.Close();

            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error reading file: {ex.Message}");
            }


        }
        return nodeAttributes;

    }


    private async Task<List<NodeAttribute>> IterateAllNodes()
    {
        var nodeAttributes = new List<NodeAttribute>();
        PLCOPCManager plcManager = new PLCOPCManager("");
        await plcManager.Connect();
        var nodes = plcManager.LoopNode("ns=4;i=2");
        foreach (var node in nodes)
        {
            if (!string.IsNullOrEmpty(node.Key))
            {
                nodeAttributes.Add(new NodeAttribute
                {
                    NodeId = node.Key,
                    NodeName = node.Value.NodeName,
                    NodeDeisplayName = node.Value.NodeDeisplayName,
                    NodeDescription = node.Value.NodeDescription,
                    NodeDataType = node.Value.NodeDataType.ToString()
                });
            }
        }

        plcManager.Close();

        return nodeAttributes;
    }

    private async void Browse_Click(object sender, RoutedEventArgs e)
    {
        var nAttributes = await IterateAllNodes();
        nodeAttributes.Clear();
        nodeAttributes.AddRange((IEnumerable<NodeAttribute>)nAttributes);
    }
    private void Filter_Click(object sender, RoutedEventArgs e)
    {
        var nAttributes = IterateAllNodes();
        nodeAttributes.Clear();
        nodeAttributes.AddRange((IEnumerable<NodeAttribute>)nAttributes);
    }


}