using AIFactory.ViewModel;
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

        var services = new ServiceCollection();
        services.AddSingleton<ViewModelChart>();
        services.AddSingleton<ViewModelPLCWriter>();
        services.AddSingleton<ViewModelMainWindow>();
        Services = services.BuildServiceProvider();

        base.OnStartup(e);
    }

}

