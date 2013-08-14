using TeamFlash.Delcom;

namespace TeamFlash.Commands
{
    class DelcomCommand : CommandBase
    {
        public DelcomCommand()
        {
            IsCommand("delcom", "Start monitoring a Delcom build light");
        }

        public override int Run(string[] remainingArguments)
        {
            buildLight = new DelcomBuildLight();
            return base.Run(remainingArguments);
        }
    }
}
