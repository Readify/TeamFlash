namespace TeamFlash.Linux
{
    class BuildLight : IBuildLight
    {
        static void SetLightColor(int color)
        {
            DelcomBuildIndicator.OpenDevice();
            DelcomBuildIndicator.SetColor(color);
            DelcomBuildIndicator.CloseDevice();
        }

        public void Success()
        {
            SetLightColor(DelcomBuildIndicator.Green);
        }

        public void Warning()
        {
            SetLightColor(DelcomBuildIndicator.Blue);
        }

        public void Fail()
        {
            SetLightColor(DelcomBuildIndicator.Red);
        }

        public void Off()
        {
            SetLightColor(DelcomBuildIndicator.Off);
        }
    }
}
