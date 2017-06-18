using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace ClientCloud
{
    class View
    {
        public View() {}

        public void DrawFileArray(JArray data)
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("File-List has been loaded:");
            for(int i=0; i < data.Count; i++)
                Console.WriteLine(data[i] + "(" + i + ")");
        }

        public void DrawWelcome()
        {
            Console.WriteLine("Usage: \n" +
                              "Type upload / download \n" +
                              "Type quit to exit");
        }

        public void DrawChoices()
        {
            Console.WriteLine("Upload files (1)");
            Console.WriteLine("Download files (2)");
        }

        public void  WriteMessage(string message, MessageType type)
        {
            if (type == MessageType.ERROR)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Error: " + message);
                ResetForegroundColor();
            }
            else if (type == MessageType.SUCCESS)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Success: " + message);
                ResetForegroundColor();
            }
            else if (type == MessageType.NOTICE)
            {
                Console.ForegroundColor = ConsoleColor.Blue;
                Console.WriteLine("Notice: " + message);
                ResetForegroundColor();
            }
        }

        private void ResetForegroundColor()
        {
            Console.ForegroundColor = ConsoleColor.Gray;
        }
    }
}
