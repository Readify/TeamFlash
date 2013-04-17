using System.Runtime.InteropServices;

namespace TeamFlash.Linux
{
    public class DelcomBuildIndicator
    {
        // USBDELVI LED COlORS
        public const int Green = 0x01;
        public const int Red = 0x02;
        public const int Blue = 0x04;
        public const int Off = 0x00;

        // Open Device
        [DllImport("delcom", EntryPoint = "delcom_OpenDevice")]
        public static extern int OpenDevice();

        // Close Device
        [DllImport("delcom", EntryPoint = "delcom_CloseDevice")]
        public static extern int CloseDevice();

        // Set LED Functions
        [DllImport("delcom", EntryPoint = "delcom_SetColor")]
        public static extern int SetColor(int color);
    }
}
