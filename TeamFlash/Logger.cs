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

        public static void Verbose(string message, params object[] parameters)
        {
            if (VerboseEnabled)
            {
                Console.WriteLine("VERBOSE: {0} {1}", DateTime.Now.ToShortTimeString(), String.Format(message, parameters));
            }
        }

        public static void Error(Exception exception)
        {
            WriteLine(exception.ToString());
        }

        public static bool VerboseEnabled { get; set; }
    }
}
