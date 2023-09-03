using System.Collections.Generic;
using System.Runtime.InteropServices;
using Avalonia.Controls;
using Avalonia.Layout;
using WebViewControl;

namespace Hypernex.CCK.ScriptEditor;

public partial class MainWindow : Window
{
    private static bool IsWindows => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
    
    internal static MainWindow? Instance;
    
    private Dictionary<string, (CrossPlatformWebView, Button)> scripts = new();
    private StackPanel stackPanel;

    public MainWindow()
    {
        InitializeComponent();
        Instance = this;
        WebSocketServerManager.OpenServer();
        HTTPHandler.Start();
        WebView.Settings.OsrEnabled = false;
        foreach (IControl control in ((Panel) Content).Children)
        {
            if (control.Name == "ScriptButtons")
                stackPanel = (StackPanel) control;
        }
    }

    private void ShowWebViewById(string id)
    {
        foreach (KeyValuePair<string, (CrossPlatformWebView, Button)> keyValuePair in
                 new Dictionary<string, (CrossPlatformWebView, Button)>(scripts))
            keyValuePair.Value.Item1.IsVisible = keyValuePair.Key == id;
    }

    public void CreateScript(string id, string name, NexboxLanguage language)
    {
        CrossPlatformWebView crossPlatformWebView;
        if (IsWindows)
            crossPlatformWebView = new CrossPlatformWebView(new WebView
            {
                Address = $"http://localhost:{HTTPHandler.Port}/web/index.html?wsport=" + WebSocketServerManager.Port +
                          "&id=" + id,
                Width = 800,
                Height = 470,
                AllowDeveloperTools = true,
                VerticalAlignment = VerticalAlignment.Bottom
            });
        else
            crossPlatformWebView =
                new CrossPlatformWebView($"http://localhost:{HTTPHandler.Port}/web/index.html?wsport=" +
                                         WebSocketServerManager.Port + "&id=" + id + "&close=true");
        Button button = new Button
        {
            Content = name + NexboxScript.GetExtensionFromLanguage(language),
            Command = new ScriptButtonCommand(() => ShowWebViewById(id))
        };
        stackPanel.Children.Add(button);
        ((Panel) Content).Children.Add(crossPlatformWebView);
        scripts.Add(id, (crossPlatformWebView, button));
        ShowWebViewById(id);
    }

    public void RemoveScript(string id)
    {
        if (scripts.ContainsKey(id))
        {
            (CrossPlatformWebView, Button) webViewOrButton = scripts[id];
            ((Panel) Content).Children.Remove(webViewOrButton.Item1);
            stackPanel.Children.Remove(webViewOrButton.Item2);
            webViewOrButton.Item1.Dispose();
            scripts.Remove(id);
        }
    }
}