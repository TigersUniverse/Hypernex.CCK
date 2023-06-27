using System;
using HttpServerLite;

namespace Hypernex.CCK.ScriptEditor;

public static class HTTPHandler
{
    internal static int Port;
    private static Webserver Server;

    private static int GetPort()
    {
        for (int i = 0; i < Program.Args.Length; i++)
        {
            string arg = Program.Args[i];
            if (arg == "-port")
                return Convert.ToInt32(Program.Args[i + 1]);
        }
        return 80;
    }

    public static void Start()
    {
        Port = GetPort();
        Server = new Webserver("127.0.0.1", Port, false, null, null, async ctx =>
        {
            string resp = "Hello from HttpServerLite!";
            ctx.Response.StatusCode = 200;
            ctx.Response.ContentLength = resp.Length;
            ctx.Response.ContentType = "text/plain";
            await ctx.Response.SendAsync(resp);
        });
        Server.Routes.Content.Add("web/", true);
        Server.Routes.Static.Add(HttpMethod.GET, "/getWSPort/",
            async context => await context.Response.SendAsync(Convert.ToString(WebSocketServerManager.Port)));
        Server.Start();
    }
}