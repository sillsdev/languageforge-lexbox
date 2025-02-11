using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FwLiteMaui;

public partial class MainPage : ContentPage
{
    public MainPage()
    {
        InitializeComponent();
    }

    internal string StartPath
    {
        get => blazorWebView.StartPath;
        set => blazorWebView.StartPath = value;
    }
}
