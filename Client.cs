using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Quobject.SocketIoClientDotNet.Client;
using Newtonsoft.Json.Linq;
using System.Windows.Forms;
using System.IO;
using Nito.AsyncEx;
using Socket = Quobject.SocketIoClientDotNet.Client.Socket;
using System.Threading;

enum Choices { UPLOAD=1, DOWNLOAD}
enum MessageType { ERROR, SUCCESS, NOTICE }

namespace ClientCloud
{
    class Client
    {
        private AutoResetEvent stopWaitHandle;
        private string m_SavePath;
        private delegate void Del(Int32[] item);
        private Del m_CurCallback;
        private Dictionary<int, string>m_Files;
        private Socket m_Socket;
        private View m_View;
        public Client(View view)
        {
            stopWaitHandle = new AutoResetEvent(false);
            m_SavePath = "";
            m_Files = new Dictionary<int, string>();
            m_View = view;
            // Fill in server IP.
            m_Socket = IO.Socket("");
        }

        public AutoResetEvent Init()
        {
            m_Socket.On(Socket.EVENT_CONNECT, () =>
            {
                m_View.DrawWelcome();
                stopWaitHandle.Set();
            });

            m_Socket.On("FolderList", (message) =>
            {
                JArray arr = (JArray)message;
                UpdateFileDic(arr);
                m_View.DrawFileArray(arr);
                stopWaitHandle.Set();
            });

            m_Socket.On("ReceiveFile", (message) =>
            {
                JArray arr = (JArray)message;
                AsyncContext.Run(() => SaveFiles(arr));
                stopWaitHandle.Set();
            });

            m_Socket.On("SuccessReceive", () =>
            {
                m_View.WriteMessage("Server saved file to storage", MessageType.SUCCESS);
                stopWaitHandle.Set();
            });

            m_Socket.On("Error", (message) =>
            {
                m_View.WriteMessage((string)message, MessageType.ERROR);
            });

            return stopWaitHandle;
        }
        // test speed synchronous vs asynchronous
        private async Task SaveFiles(JArray data)
        {
            int count = (int)data[0];

            for (int i = 0; i < count; i++)
                await SaveFile((string)data[1][i], (byte[])data[2][i]);
        }

        private async Task SaveFile(string name, byte[] data)
        {
            if (data.Length == 0)
            {
                m_View.WriteMessage("Reading data for File: " + name, MessageType.ERROR);
                return;
            }
            try
            {
                using (FileStream SourceStream = File.Open((m_SavePath + name), FileMode.Create))
                {
                    await SourceStream.WriteAsync(data, 0, data.Length);
                    m_View.WriteMessage("File received and saved: "+ name, MessageType.SUCCESS);
                }
            }
            catch (Exception ex)
            {
                m_View.WriteMessage(ex.Message, MessageType.ERROR);
                m_View.WriteMessage("Saving file: " + name, MessageType.ERROR);
            }
        }

        private void InitialChoice(Int32[] choices)
        {
            if (choices[0] == (int)Choices.DOWNLOAD)
            {
                Thread thread = new Thread(OpenFolderDialog);
                thread.IsBackground = true;
                thread.SetApartmentState(ApartmentState.STA);
                thread.Start();

                thread.Join();
            }
            else if (choices[0] == (int)Choices.UPLOAD)
            {
                Thread thread = new Thread(OpenFileDialog);
                thread.IsBackground = true;
                thread.SetApartmentState(ApartmentState.STA);
                thread.Start();

                thread.Join();
            }
        }

        private void OpenFileDialog()
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.InitialDirectory = "c:\\";
            dialog.Filter = "Text files/Images|*.txt;*.png;*.jpg;*.jpeg;";
            dialog.FilterIndex = 1;
            dialog.RestoreDirectory = true;

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    string filename = Path.GetFileName(dialog.FileName);
                    byte[] data = File.ReadAllBytes(dialog.FileName);
                    SendFile(filename, data);
                    m_View.WriteMessage("Sent file data to server. Waiting for response", MessageType.NOTICE);
                    stopWaitHandle.WaitOne();
                    m_CurCallback = InitialChoice;
                }
                catch (Exception e)
                {
                    Console.WriteLine("Error trying to convert char to int: " + e.GetType());
                    Console.WriteLine(e.Message);
                }
            }
        }

        private void OpenFolderDialog()
        {
            FolderBrowserDialog dialog = new FolderBrowserDialog();
            dialog.Description = "Folder to save Files  to";

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                m_SavePath = dialog.SelectedPath + "//";
                GetFolderList();
                stopWaitHandle.WaitOne();
                m_CurCallback = GetFiles;
            }
        }

        private void GetFolderList()
        {
            m_Socket.Emit("GetFolders");
        }

        private void SendFile(string name, Byte[] data)
        {
            m_Socket.Emit("SendFile", name, data);
        }

        private void GetFiles(Int32[] choices)
        {
            JArray filenames = new JArray();

            for (int i = 0; i < choices.Length; i++)
            {
                if (choices[i] < m_Files.Count)
                {
                    filenames.Add(m_Files[choices[i]]);
                }
                else
                {
                    m_View.WriteMessage("Number out of range! Try again", MessageType.ERROR);
                    m_CurCallback = GetFiles;
                    return;
                }
            }
            m_Socket.Emit("GetFiles", filenames);
            m_View.WriteMessage("Waiting for file data...", MessageType.NOTICE);
            stopWaitHandle.WaitOne();
            m_View.DrawChoices();
            m_CurCallback = InitialChoice;
        }

        private void UpdateFileDic(JArray arr)
        {
            m_Files.Clear();
            for (int i = 0; i < arr.Count; i++)
                m_Files[i] = (string)arr[i];
        }

        private void UILoop()
        {
            string input = "";
            Int32[] input_arr;
            while(true)
            {
                input = Console.ReadLine();
                if (input == "quit")
                {
                    return;
                }
                else if (m_CurCallback == InitialChoice)
                {
                    if (input == "upload")
                        m_CurCallback(new Int32[] { 1 });
                    else if (input == "download")
                        m_CurCallback(new Int32[] { 2 });
                    else
                        m_View.WriteMessage("Wrong command. Try again", MessageType.ERROR);
                }
                else if (m_CurCallback == GetFiles)
                {
                    input_arr = ToIntArray(input);
                    if (input_arr.Length > 0)
                        m_CurCallback(input_arr);
                }
            }
        }

        private Int32[] ToIntArray(string input)
        {
            char[] input_arr = input.ToCharArray();
            Int32[] final_arr = new Int32[input_arr.Length];
            Int32 temp = 0;
            for (int i=0; i < input_arr.Length; i++)
            {
                temp = (int)Char.GetNumericValue(input_arr[i]);
                if (temp == -1)
                {
                    m_View.WriteMessage("Please only enter Numbers!", MessageType.ERROR);
                    return new Int32[0];
                }
                final_arr[i] = temp;
            }    
            return final_arr;
        }

        public void StartUiLoop()
        {
            m_CurCallback = InitialChoice;
            UILoop();
        }
    }
}
