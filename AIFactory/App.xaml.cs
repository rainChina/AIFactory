using AIFactory.Util;
using AIFactory.View;
using AIFactory.ViewModel;
using HandyControl.Tools;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using Microsoft.Extensions.DependencyInjection;
using SkiaSharp;
using System.Configuration;
using System.Data;
using System.Windows;

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

        var setupWindow = new WinStartup();
        bool? result = setupWindow.ShowDialog();

        if (result == true)
        {
            try
            {
                mainWindow.ShowDialog();
            }
            catch(Exception ex)
            {
                HandyControl.Controls.MessageBox.Show(ex.Message, "错误");
            }
        }
        else
        {
            Shutdown();
        }
    }




}

