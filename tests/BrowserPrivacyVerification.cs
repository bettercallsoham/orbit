using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Orbit.Services;

namespace Orbit.Tests;

public static class BrowserPrivacyVerification
{
    public static async Task RunAsync(string directory)
    {
        if (!BrowserRenderService.IsAvailable)
        {
            Console.WriteLine("[privacy] skipped browser check: Edge/Chrome unavailable");
            return;
        }

        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        int port = ((IPEndPoint)listener.LocalEndpoint).Port;
        string page = Path.Combine(directory, "privacy-check.html");
        string output = Path.Combine(directory, "privacy-check.pdf");
        await File.WriteAllTextAsync(page, $$"""
            <!doctype html><html><body><h1>Local document</h1>
            <img src="http://127.0.0.1:{{port}}/remote-image">
            <script>fetch('http://127.0.0.1:{{port}}/upload',{method:'POST',mode:'no-cors',body:'private-content'})</script>
            </body></html>
            """);
        await BrowserRenderService.PrintAsync(page, output);
        if (!File.Exists(output) || listener.Pending())
            throw new Exception("Browser rendering attempted a page-origin network request or failed to render locally.");
        Console.WriteLine("[privacy] local browser render completed with no loopback image/script request");
    }
}
