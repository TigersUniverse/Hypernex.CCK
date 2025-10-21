using System;
using System.IO;
using Hypernex.Networking.SandboxedClasses;
using Nexbox;
using UnityEngine;
using YoutubeDLSharp;
using YoutubeDLSharp.Metadata;
using Logger = Hypernex.CCK.Logger;

namespace Hypernex.Networking.Server.SandboxedClasses
{
    public static class Streaming
    {
        internal static YoutubeDL ytdlp = new();
        
        private static bool IsStream(Uri uri)
        {
            try
            {
                bool isStream = false;
                switch (uri.Scheme.ToLower())
                {
                    case "rtmp":
                    case "rtsp":
                    case "srt":
                    case "udp":
                    case "tcp":
                        isStream = true;
                        break;
                }

                if (isStream) return true;
                string fileName = Path.GetFileName(uri.LocalPath);
                string ext = Path.GetExtension(fileName);
                switch (ext)
                {
                    case ".m3u8":
                    case ".mpd":
                    case ".flv":
                        isStream = true;
                        break;
                }

                return isStream;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static bool NeedsClientFetch(string hostname)
        {
            switch (hostname.ToLower())
            {
                case "youtube.com":
                case "www.youtube.com":
                case "youtu.be":
                case "m.youtube.com":
                    return true;
            }
            return false;
        }
        
        public static async void GetWithOptions(string url, object onDone, StreamDownloadOptions options)
        {
            try
            {
                Logger.CurrentLogger.Log(url);
                VideoRequest videoRequest = VideoRequestHelper.Create(url, options);
                Uri uri = new Uri(url);
                if (NeedsClientFetch(uri.Host))
                {
                    VideoRequestHelper.SetDownloadUrl(ref videoRequest, url);
                    VideoRequestHelper.SetNeedsClientFetch(ref videoRequest, true);
                    SandboxFuncTools.InvokeSandboxFunc(SandboxFuncTools.TryConvert(onDone), videoRequest);
                    return;
                }
                RunResult<VideoData> metaResult = await ytdlp.RunVideoDataFetch(url);
                string liveUrl = String.Empty;
                if (metaResult.Success)
                {
                    switch (metaResult.Data.LiveStatus)
                    {
                        case LiveStatus.IsLive:
                            liveUrl = metaResult.Data.Url;
                            break;
                        case LiveStatus.IsUpcoming:
                            throw new Exception("Invalid LiveStream!");
                    }
                }
                if (!string.IsNullOrEmpty(liveUrl))
                {
                    VideoRequestHelper.SetIsStream(ref videoRequest, true);
                    VideoRequestHelper.SetDownloadUrl(ref videoRequest, liveUrl);
                    SandboxFuncTools.InvokeSandboxFunc(SandboxFuncTools.TryConvert(onDone), videoRequest);
                    return;
                }
                if (IsStream(uri))
                {
                    VideoRequestHelper.SetIsStream(ref videoRequest, true);
                    VideoRequestHelper.SetDownloadUrl(ref videoRequest, liveUrl);
                    SandboxFuncTools.InvokeSandboxFunc(SandboxFuncTools.TryConvert(onDone), videoRequest);
                    return;
                }
                VideoRequestHelper.SetDownloadUrl(ref videoRequest, metaResult.Data.Url);
                SandboxFuncTools.InvokeSandboxFunc(SandboxFuncTools.TryConvert(onDone), videoRequest);
            }
            catch (Exception e)
            {
                Logger.CurrentLogger.Critical(e);
                SandboxFuncTools.InvokeSandboxFunc(SandboxFuncTools.TryConvert(onDone));
            }
        }

        public static void Get(string url, object onDone) => GetWithOptions(url, onDone, new StreamDownloadOptions());
    }
}