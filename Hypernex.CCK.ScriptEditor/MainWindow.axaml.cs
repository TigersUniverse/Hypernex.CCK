using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Layout;
using WebViewControl;

namespace Hypernex.CCK.ScriptEditor;

public partial class MainWindow : Window
{
    internal static MainWindow? Instance;

    private Dictionary<string, (WebView, Button)> scripts = new();
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
        foreach (KeyValuePair<string, (WebView, Button)> keyValuePair in
                 new Dictionary<string, (WebView, Button)>(scripts))
            keyValuePair.Value.Item1.IsVisible = keyValuePair.Key == id;
    }

    public void CreateScript(string id, string name, NexboxLanguage language)
    {
        WebView webView = new WebView
        {
            Address = $"http://localhost:{HTTPHandler.Port}/web/index.html?wsport=" + WebSocketServerManager.Port + "&id=" + id,
            Width = 800,
            Height = 470,
            AllowDeveloperTools = true,
            VerticalAlignment = VerticalAlignment.Bottom
        };
        Button button = new Button
        {
            Content = name + NexboxScript.GetExtensionFromLanguage(language),
            Command = new ScriptButtonCommand(() => ShowWebViewById(id))
        };
        stackPanel.Children.Add(button);
        ((Panel) Content).Children.Add(webView);
        scripts.Add(id, (webView, button));
        ShowWebViewById(id);
    }

    public void RemoveScript(string id)
    {
        if (scripts.ContainsKey(id))
        {
            (WebView, Button) webViewOrButton = scripts[id];
            ((Panel) Content).Children.Remove(webViewOrButton.Item1);
            stackPanel.Children.Remove(webViewOrButton.Item2);
            webViewOrButton.Item1.Dispose();
            scripts.Remove(id);
        }
    }
}