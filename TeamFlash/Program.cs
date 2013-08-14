using System;
using System.Collections.Generic;
using System.Linq;
using ManyConsole;

namespace TeamFlash
{
    class Program
    {
        static int Main(string[] args)
        {
            //We have to register this first...
            ILBundle.RegisterAssemblyResolver();
            return Run(args);
        }

        private static int Run(string[] args)
        {
            var commands = GetCommands().Where(c => c.GetType().Name != "CommandBase");
            return ConsoleCommandDispatcher.DispatchCommand(commands, args, Console.Out);
        }

        private static IEnumerable<ConsoleCommand> GetCommands()
        {
            return ConsoleCommandDispatcher.FindCommandsInSameAssemblyAs(typeof(Program));
        }
        
    }
}
