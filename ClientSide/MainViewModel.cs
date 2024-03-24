using Client.Command;
using ClientSide;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;


public class MainViewModel
{
    public MainView? MainView { get; set; }

    private UdpClient client;
    private readonly IPAddress clientIP;
    private readonly IPEndPoint remoteEP;
    private readonly int port;
    private bool isStart = false;
    private bool isFirst = true;
    private bool isStop = false;
    public ICommand? StartCommand { get; set; }

    public MainViewModel(MainView mainView)
    {
        MainView = mainView;
        client = new UdpClient();
        clientIP = IPAddress.Parse("127.0.0.1");
        port = 27001;
        remoteEP = new IPEndPoint(clientIP, port);

        StartCommand = new RelayCommand(
            async a =>
            {
                if (!isStart)
                {
                    isStart = true;
                    MainView!.button.Content = "Stop";
                    MainView.button.Background = Brushes.DarkRed;

                    await StartReceivingImages();
                }
                else
                {
                    isStart = false;
                    MainView!.button.Content = "Start";
                    MainView.button.Background = Brushes.Green;
                    isStop = true;
                }
            },
            pre => true);
    }


    private async Task StartReceivingImages()
    {
        var maxLen = ushort.MaxValue - 29;
        var len = 0;
        var buffer = new byte[maxLen];

        if (isFirst)
        {
            await client.SendAsync(buffer, buffer.Length, remoteEP);
            isFirst = false;
        }

        var Image = new List<byte>();
        try
        {
            while (true)
            {
                if (isStop)
                {
                    isStop = false;
                    break;
                }
                do
                {
                    var result = await client.ReceiveAsync();
                    buffer = result.Buffer;
                    len = buffer.Length;
                    Image.AddRange(buffer);
                } while (len == maxLen);

                var image = await ByteToImageAsync(Image.ToArray());

                if (image is not null)
                    MainView!.ImageShare.Source = image;

                Image.Clear();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message);
        }
    }

    private static async Task<BitmapImage?> ByteToImageAsync(byte[]? imageData)
    {
        var image = new BitmapImage();
        image.BeginInit();
        image.StreamSource = new MemoryStream(imageData!);
        image.CacheOption = BitmapCacheOption.OnLoad;
        image.EndInit();

        await Task.Delay(1);
        return image;
    }
}