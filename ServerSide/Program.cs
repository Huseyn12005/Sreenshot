using System.Net;
using System.Net.Sockets;
using System.Drawing;
using System.Drawing.Imaging;

namespace Server;

public class Program
{
    private static List<UdpClient> activeClients = new List<UdpClient>();
    public static async Task Main()
    {
        var ip = IPAddress.Parse("127.0.0.1");
        var port = 27001;

        var listenerEP = new IPEndPoint(ip, port);
        var listener = new UdpClient(listenerEP);

        while (true)
        {

            var result = await listener.ReceiveAsync();
            Console.WriteLine($"{result.RemoteEndPoint} Connected...");

            var clientEP = result.RemoteEndPoint;
            var client = new UdpClient();
            activeClients.Add(client);

            _ = Task.Run(async () =>
            {
                var clientEP = result.RemoteEndPoint;


                while (true)
                {
                    var imageScreen = await TakeScreenShotAsync();
                    var imageBytes = await ImageToByteAsync(imageScreen);

                    if (imageBytes != null)
                    {
                        var chunks = imageBytes.Chunk(ushort.MaxValue - 29);

                        foreach (var chunk in chunks)
                        {
                            try
                            {
                                await client.SendAsync(chunk, chunk.Length, clientEP);
                            }
                            catch (SocketException)
                            {
                                activeClients.Remove(client);
                                break;
                            }
                        }
                    }
                }
            });
        }
    }

    private static async Task<Image?> TakeScreenShotAsync()
    {
        Bitmap? bitmap = new Bitmap(1920,1080);

        await Task.Run(() =>
        {
            using Graphics graphics = Graphics.FromImage(bitmap);
            graphics.CopyFromScreen(0, 0, 0, 0, bitmap.Size);
        });

        return bitmap;

    }

    private static async Task<byte[]?> ImageToByteAsync(Image? image)
    {
        using MemoryStream ms = new MemoryStream();
        await Task.Run(() => image.Save(ms, ImageFormat.Jpeg));

        return ms.ToArray();
    }
}

