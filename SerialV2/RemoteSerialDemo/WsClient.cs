using System;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RemoteDemo
{
    public class WsClient : IDisposable
    {
        public event EventHandler<WsClient, string> TextMessageReceived;
        public event EventHandler<WsClient, Stream> BinaryMessageReceived;
        public int ReceiveBufferSize { get; set; } = 8192;

        public async Task ConnectAsync(string url)
        {
            if (WS != null)
            {
                if (WS.State == WebSocketState.Open) return;
                else WS.Dispose();
            }
            WS = new ClientWebSocket();
            if (CTS != null) CTS.Dispose();
            CTS = new CancellationTokenSource();
            await WS.ConnectAsync(new Uri(url), CTS.Token);
            await Task.Factory.StartNew(ReceiveLoop, CTS.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        }

        public async Task DisconnectAsync()
        {
            if (WS is null) return;
            // TODO: requests cleanup code, sub-protocol dependent.
            if (WS.State == WebSocketState.Open)
            {
                CTS.CancelAfter(TimeSpan.FromSeconds(2));
                await WS.CloseOutputAsync(WebSocketCloseStatus.Empty, "", CancellationToken.None);
                await WS.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
            }
            WS.Dispose();
            WS = null;
            CTS.Dispose();
            CTS = null;
        }

        private async Task ReceiveLoop()
        {
            var loopToken = CTS.Token;
            MemoryStream outputStream = null;
            WebSocketReceiveResult receiveResult = null;
            var buffer = new byte[ReceiveBufferSize];
            try
            {
                while (!loopToken.IsCancellationRequested)
                {
                    outputStream = new MemoryStream(ReceiveBufferSize);
                    do
                    {
                        receiveResult = await WS.ReceiveAsync(new ArraySegment<byte>(buffer), CTS.Token);
                        if (receiveResult.MessageType != WebSocketMessageType.Close)
                            outputStream.Write(buffer, 0, receiveResult.Count);
                    }
                    while (!receiveResult.EndOfMessage);
                    if (receiveResult.MessageType == WebSocketMessageType.Close) break;
                    outputStream.Position = 0;
                    if (receiveResult.MessageType == WebSocketMessageType.Text)
                        TextResponseReceived(outputStream);
                    else
                        BinaryMessageReceived?.Invoke(this, outputStream);
                }
            }
            catch (TaskCanceledException) { }
            finally
            {
                outputStream?.Dispose();
            }
        }

        private async Task SendMessageAsync(string message)
        {
            await WS.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(message)), WebSocketMessageType.Text, true, CancellationToken.None);
        }

        private async Task SendBinaryMessageAsync(int dataType,byte[] data)
        {
            await WS.SendAsync(new ArraySegment<byte>(BitConverter.GetBytes(dataType)), WebSocketMessageType.Binary, false, CancellationToken.None);
            await WS.SendAsync(new ArraySegment<byte>(data), WebSocketMessageType.Binary, true, CancellationToken.None);
        }


        private void TextResponseReceived(Stream inputStream)
        {
            using (StreamReader reader = new StreamReader(inputStream))
            {
                var input = reader.ReadToEnd();
                TextMessageReceived?.Invoke(this, input);
            }

        }

        public void SendMessage(string msg) => SendMessageAsync(msg).Wait();
        public void SendCommand(object msg) => SendMessageAsync("command:"+msg.ToJson()).Wait();
        public void SendMessage(string cmd , object msg) => SendMessageAsync(cmd+":" + msg.ToJson()).Wait();

        public void SendBinaryMessage(int dataType,byte[] data) => SendBinaryMessageAsync(dataType,data).Wait();
        public void Dispose() => DisconnectAsync().Wait();

        private ClientWebSocket WS;
        private CancellationTokenSource CTS;

    }
}
