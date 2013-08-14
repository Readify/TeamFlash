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
            ILBundle.RegisterAssemblyResolver();
            return Run(args);
        }

        private static int Run(string[] args)
        {
            // locate any commands in the assembly (or use an IoC container, or whatever source)
            var commands = GetCommands().Where(c => c.GetType().Name != "CommandBase");

            // then run them.
            return ConsoleCommandDispatcher.DispatchCommand(commands, args, Console.Out);
        }

        private static IEnumerable<ConsoleCommand> GetCommands()
        {
            return ConsoleCommandDispatcher.FindCommandsInSameAssemblyAs(typeof(Program));
        }
        
    }
}
