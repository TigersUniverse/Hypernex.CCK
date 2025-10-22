using System;
using System.IO;
using Hypernex.CCK;
using Hypernex.CCK.Unity.Emulator;
using Hypernex.Networking.SandboxedClasses;
using Nexbox;
using YoutubeDLSharp;
using YoutubeDLSharp.Metadata;
using YoutubeDLSharp.Options;

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
                // EMULATOR ONLY
                OptionSet optionSet = new OptionSet
                {            
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN || UNITY_MAC
                    Format = "bestvideo[vcodec=vp8]/bestvideo[vcodec=h264]+bestaudio/best"
#else
                    Format = "bestvideo[vcodec=vp8]+bestaudio/best"
#endif
                    , ExtractorArgs = "youtube:player_client=default,web_safari;player_js_version=actual"
                };
                RunResult<string> download = options.AudioOnly
                    ? await ytdlp.RunAudioDownload(url, overrideOptions: optionSet)
                    : await ytdlp.RunVideoDownload(url, overrideOptions: optionSet);
                if (!download.Success)
                {
                    foreach (string s in download.ErrorOutput)
                        Logger.CurrentLogger.Error(s);
                    if(string.IsNullOrEmpty(download.Data) || !File.Exists(download.Data))
                        throw new Exception("Failed to get data!");
                }
                string newFileLocation =
                    Path.Combine(Init.Instance.GetMediaLocation(), Path.GetFileName(download.Data));
                File.Move(download.Data!, newFileLocation);
                VideoRequestHelper.SetDownloadUrl(ref videoRequest, newFileLocation);
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