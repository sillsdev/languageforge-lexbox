using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FwLiteMaui;

public partial class MainPage : ContentPage
{

    public event BackButtonHandler? BackButtonPressed;

    public MainPage()
    {
        InitializeComponent();
    }

    protected override bool OnBackButtonPressed()
    {
        if (BackButtonPressed != null)
        {
            foreach (var handler in BackButtonPressed.GetInvocationList())
            {
                if (((BackButtonHandler)handler)())
                    return true;
            }
        }
        return false;
    }

    internal string StartPath
    {
        get => blazorWebView.StartPath;
        set => blazorWebView.StartPath = value;
    }
}
