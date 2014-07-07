namespace TeamFlash
{
    class BuildLight : IBuildLight
    {
        readonly Monitor _monitor = new Monitor();

        public void Success()
        {
            VerboseThemeChange("GREEN");
            _monitor.SetLed(DelcomBuildIndicator.REDLED, false, false);
            _monitor.SetLed(DelcomBuildIndicator.GREENLED, true, false);
            _monitor.SetLed(DelcomBuildIndicator.BLUELED, false, false);
        }

        public void Warning()
        {
            VerboseThemeChange("AMBER");
            _monitor.SetLed(DelcomBuildIndicator.REDLED, false, false);
            _monitor.SetLed(DelcomBuildIndicator.GREENLED, false, false);
            _monitor.SetLed(DelcomBuildIndicator.BLUELED, true, false);
        }

        public void Fail()
        {
            VerboseThemeChange("RED");
            _monitor.SetLed(DelcomBuildIndicator.REDLED, true, false);
            _monitor.SetLed(DelcomBuildIndicator.GREENLED, false, false);
            _monitor.SetLed(DelcomBuildIndicator.BLUELED, false, false);
        }

        public void Off()
        {
            VerboseThemeChange("OFF");
            _monitor.SetLed(DelcomBuildIndicator.REDLED, false, false);
            _monitor.SetLed(DelcomBuildIndicator.GREENLED, false, false);
            _monitor.SetLed(DelcomBuildIndicator.BLUELED, false, false);
        }

        static void VerboseThemeChange(string newTheme)
        {
            Logger.Verbose("Switching LED to {0}.", newTheme);
        }
    }
}