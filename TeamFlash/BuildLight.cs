namespace TeamFlash
{
    class BuildLight : IBuildLight
    {
        private readonly Monitor monitor = new Monitor();

        public void Success()
        {
            monitor.SetLed(DelcomBuildIndicator.REDLED, false, false);
            monitor.SetLed(DelcomBuildIndicator.GREENLED, true, false);
            monitor.SetLed(DelcomBuildIndicator.BLUELED, false, false);
        }

        public void Warning()
        {
            monitor.SetLed(DelcomBuildIndicator.REDLED, false, false);
            monitor.SetLed(DelcomBuildIndicator.GREENLED, false, false);
            monitor.SetLed(DelcomBuildIndicator.BLUELED, true, false);
        }

        public void Fail()
        {
            monitor.SetLed(DelcomBuildIndicator.REDLED, true, false);
            monitor.SetLed(DelcomBuildIndicator.GREENLED, false, false);
            monitor.SetLed(DelcomBuildIndicator.BLUELED, false, false);
        }

        public void Off()
        {
            monitor.SetLed(DelcomBuildIndicator.REDLED, false, false);
            monitor.SetLed(DelcomBuildIndicator.GREENLED, false, false);
            monitor.SetLed(DelcomBuildIndicator.BLUELED, false, false);
        }
    }
}