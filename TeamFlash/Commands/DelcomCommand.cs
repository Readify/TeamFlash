using TeamFlash.Delcom;
using TeamFlash.TeamCity;

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
            BuildLight = new DelcomBuildLight();
            return base.Run(remainingArguments);
        }

        protected override void RegisterBuildEvents(TeamCityBuildMonitor buildMonitor, int blinkInterval)
        {
            buildMonitor.BuildChecked += (sender, eventArgs) => BuildLight.Blink();
            buildMonitor.BuildPaused += (sender, eventArgs) => BuildLight.BlinkThenRevert(LightColour.Yellow, blinkInterval);
            buildMonitor.BuildSkipped += (sender, eventArgs) => BuildLight.BlinkThenRevert(LightColour.Purple, blinkInterval);
            buildMonitor.BuildSuccess += (sender, eventArgs) => BuildLight.BlinkThenRevert(LightColour.Green, blinkInterval);
            buildMonitor.BuildFail += (sender, eventArgs) => BuildLight.BlinkThenRevert(LightColour.Red, blinkInterval);
            buildMonitor.BuildUnknown += (sender, eventArgs) => BuildLight.BlinkThenRevert(LightColour.Yellow, blinkInterval);
        }
    }
}
