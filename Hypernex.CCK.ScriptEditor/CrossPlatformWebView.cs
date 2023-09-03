using System;
using System.Diagnostics;
using Avalonia.Controls;
using WebViewControl;

namespace Hypernex.CCK.ScriptEditor;

public class CrossPlatformWebView
{
    internal WebView? WebView;
    private string Url = String.Empty;

    private bool v;
    public bool IsVisible
    {
        get
        {
            if (WebView != null)
                return WebView.IsVisible;
            return v;
        }
        set
        {
            if (WebView != null)
            {
                WebView.IsVisible = value;
                return;
            }
            v = value;
            if (v)
            {
                ProcessStartInfo sInfo = new ProcessStartInfo(Url){UseShellExecute = true};  
                Process.Start(sInfo);
            }
        }
    }

    public CrossPlatformWebView(WebView webView) => WebView = webView;
    public CrossPlatformWebView(string url) => Url = url;

    public void Dispose() => WebView?.Dispose();
}

public static class WebViewExtensions
{
    public static void Add(this Controls controls, CrossPlatformWebView crossPlatformWebView)
    {
        if (crossPlatformWebView.WebView == null)
            return;
        controls.Add(crossPlatformWebView.WebView);
    }
    
    public static void Remove(this Controls controls, CrossPlatformWebView crossPlatformWebView)
    {
        if (crossPlatformWebView.WebView == null)
            return;
        controls.Remove(crossPlatformWebView.WebView);
    }
}