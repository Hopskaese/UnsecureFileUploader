using System.Threading.Tasks;
using System.Threading;

namespace ClientCloud
{
    class Program
    {
        static void Main(string[] args)
        {
            TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>();
            View view = new View();
            Client client = new Client(view);
            AutoResetEvent stopWaitHandle = client.Init();
            stopWaitHandle.WaitOne();
            client.StartUiLoop();
        }
    }
}
