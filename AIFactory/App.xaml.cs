using AIFactory.Message;
using AIFactory.Util;
using AIFactory.View;
using AIFactory.ViewModel;
using CommunityToolkit.Mvvm.Messaging;
using HandyControl.Tools;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using Microsoft.Extensions.DependencyInjection;
using SkiaSharp;
using System.Configuration;
using System.Data;
using System.IO;
using System.Security.Cryptography;
using System.Windows;
using System.Xml.Serialization;
using static OpenTK.Graphics.OpenGL.GL;

namespace AIFactory;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{

    public static IServiceProvider Services { get; private set; }
    protected override void OnStartup(StartupEventArgs e)
    {
        LiveCharts.Configure(config =>
              config
                  // you can override the theme 
                  // .AddDarkTheme()  

                  // In case you need a non-Latin based font, you must register a typeface for SkiaSharp
                  .HasGlobalSKTypeface(SKFontManager.Default.MatchCharacter('汉')) // <- Chinese 
                                                                                  //.HasGlobalSKTypeface(SKFontManager.Default.MatchCharacter('あ')) // <- Japanese 
                                                                                  //.HasGlobalSKTypeface(SKFontManager.Default.MatchCharacter('헬')) // <- Korean 
                                                                                  //.HasGlobalSKTypeface(SKFontManager.Default.MatchCharacter('Ж'))  // <- Russian 

              //.HasGlobalSKTypeface(SKFontManager.Default.MatchCharacter('أ'))  // <- Arabic 
              //.UseRightToLeftSettings() // Enables right to left tooltips 

              );
        ConfigHelper.Instance.SetLang("zh-cn");
        var services = new ServiceCollection();
        services.AddSingleton<ViewModelChart>();
        services.AddSingleton<ViewModelPLCWriter>();
        services.AddSingleton<ViewModelMainWindow>();
        services.AddSingleton<ViewModelStartup>();
        services.AddSingleton<UserPreference>();
        Services = services.BuildServiceProvider();

        base.OnStartup(e);
        var mainWindow = new MainWindow();
        bool? result = true;
        if (!UserPreferenceLoad())
        {
            var setupWindow = new WinStartup();
            result = setupWindow.ShowDialog();
        }

        if (result == true)
        {
            try
            {
                TaskStartMessage msg = new TaskStartMessage(true);
                WeakReferenceMessenger.Default.Send(msg);
                mainWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                HandyControl.Controls.MessageBox.Show(ex.Message, "错误");
            }
        }
        else
        {
            Shutdown();
        }
    }


    private bool UserPreferenceLoad()
    {
        if (File.Exists(UserPreference.FileName))
        {
            XmlSerializer serializer = new XmlSerializer(typeof(UserPreference));

            // 读取 XML 文件并反序列化
            using (StreamReader reader = new StreamReader(UserPreference.FileName))
            {
                // 将读取的内容转换为目标类型
                var res = serializer.Deserialize(reader) as UserPreference;

                if (res != null)
                {
                    UserPreference.Instance.Copy(res);
                    return true;
                }
                else { return false; }
            }
        }
        else
        {
            return false;
        }

    }

}

