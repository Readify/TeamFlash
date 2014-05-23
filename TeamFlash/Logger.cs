using System;

namespace TeamFlash
{
    public class Logger
    {
        public static void WriteLine(string message, params object[] parameters)
        {
            var messageToLog = String.Format(message, parameters);
            Console.WriteLine("{0} {1}", DateTime.Now.ToShortTimeString(), messageToLog);
        }
    }
}
