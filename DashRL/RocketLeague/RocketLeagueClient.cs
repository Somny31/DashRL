using System;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DashRL.RocketLeague;

public class RocketLeagueClient
{
    private readonly ClientWebSocket _webSocket = new();

    private const string WebSocketUrl = "ws://127.0.0.1:49124";

    // Cet événement permettra à MainWindow de recevoir les messages.
    public event Action<string>? MessageReceived;

    public async Task<bool> ConnectAsync()
    {
        try
        {
            await _webSocket.ConnectAsync(
                new Uri(WebSocketUrl),
                CancellationToken.None
            );

            if (_webSocket.State != WebSocketState.Open)
                return false;

            // On démarre l'écoute sans bloquer l'interface.
            _ = ReceiveMessagesAsync();

            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erreur WebSocket : {ex.Message}");
            return false;
        }
    }

    private async Task ReceiveMessagesAsync()
    {
        byte[] buffer = new byte[8192];

        try
        {
            while (_webSocket.State == WebSocketState.Open)
            {
                using MemoryStream messageStream = new();

                WebSocketReceiveResult result;

                do
                {
                    result = await _webSocket.ReceiveAsync(
                        new ArraySegment<byte>(buffer),
                        CancellationToken.None
                    );

                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        await _webSocket.CloseAsync(
                            WebSocketCloseStatus.NormalClosure,
                            "Connexion fermée",
                            CancellationToken.None
                        );

                        return;
                    }

                    messageStream.Write(
                        buffer,
                        0,
                        result.Count
                    );

                } while (!result.EndOfMessage);

                string message = Encoding.UTF8.GetString(
                    messageStream.ToArray()
                );

                MessageReceived?.Invoke(message);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erreur de réception : {ex.Message}");
        }
    }
}