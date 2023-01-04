// -----------------------------------------------------------------------
// <copyright file="SocketLivenessHostedService.cs" company="Petabridge, LLC">
//      Copyright (C) 2015 - 2023 Petabridge, LLC <https://petabridge.com>
// </copyright>
// -----------------------------------------------------------------------

using System.Net;
using System.Net.Sockets;
using System.Text;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Akka.HealthCheck.Hosting.Example.Client;

public interface ILivenessProbe: IHostedService
{ }

public interface IReadinessProbe: IHostedService
{ }

public class SocketHealthHostedService: ILivenessProbe, IReadinessProbe
{
    private readonly CancellationTokenSource _shutdownCts = new ();
    private Task? _runTask;
    private readonly string _name;
    private readonly int _port;

    public SocketHealthHostedService(string name, int port)
    {
        _name = name;
        _port = port;
    }
    
    public Task StartAsync(CancellationToken cancellationToken)
    {
        _runTask = StartService();
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _shutdownCts.Cancel();
        if(_runTask is { })
            await _runTask;
        _shutdownCts.Dispose();
    }

    private async Task StartService()
    {
        var buffer = new byte[256];
        var endpoint = new IPEndPoint(IPAddress.Loopback, _port);
        while (!_shutdownCts.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(2000, _shutdownCts.Token);
            }
            catch
            {
                // no op
            }

            if (_shutdownCts.IsCancellationRequested)
                break;
            
            using var clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(_shutdownCts.Token);
            timeoutCts.CancelAfter(TimeSpan.FromMilliseconds(500));
            try
            {
                await clientSocket.ConnectAsync(endpoint, timeoutCts.Token);
            }
            catch (TaskCanceledException)
            {
                if (_shutdownCts.IsCancellationRequested)
                {
                    // Application is shutting down
                    break;
                }
                Console.WriteLine($"{_name}: Not Healthy, Connection timed out");
            }
            catch (Exception e)
            {
                Console.WriteLine($"{_name}: Not Healthy, {e}");
                continue;
            }

            if (clientSocket.Connected)
            {
                var received = clientSocket.Receive(buffer);
                var message = Encoding.UTF8.GetString(buffer, 0, received);
                if(message == "akka.net")
                    Console.WriteLine($"{_name}: Healthy");
                else
                    Console.WriteLine($"{_name}: Not Healthy, invalid response: {message}");
                await clientSocket.DisconnectAsync(true, _shutdownCts.Token);
            }
            else
            {
                Console.WriteLine($"{_name}: Not Healthy, socket connection failed");
            }
        }
    }
}