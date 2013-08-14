using System;
using System.Collections.Generic;
using System.Linq;
using Q42.HueApi;

namespace TeamFlash.Hue
{
    class HueBuildLight : IBuildLight
    {
        private readonly HueClient _hueClient;
        private readonly List<string> _lights;
        public LightColour CurrentLightColour { get; private set; }

        public static string AppKey
        {
            get { return "beatsadelcomanyday"; }
        }

        public static string AppName
        {
            get { return "TeamFlash"; }
        }

        public HueBuildLight(string ip,IEnumerable<string> lights)
        {
            _hueClient = new HueClient(ip);
            _hueClient.Initialize(AppKey);
            _lights = lights.ToList();
        }

        public void TestLights()
        {
            SendCommand("FF00AA");
        }

        private void SendCommand(string colour)
        {
            var command = new LightCommand();
            command.TurnOn().SetColor(colour);
            command.Alert = Alert.Once;
            command.Effect = Effect.ColorLoop;
            _hueClient.SendCommandAsync(command, _lights);
        }


        public void TurnOnSuccessLight()
        {
            SendCommand("FF00AA");
        }

        public void TurnOnWarningLight()
        {
            SendCommand("FF00AA");
        }

        public void TurnOnFailLight()
        {
            SendCommand("AAAAAA");
        }

        public void TurnOffLights()
        {
            SendCommand("999999");
        }

        public void Blink()
        {
            SendCommand("FF00AA");
        }

        public void BlinkThenRevert(LightColour lightColour, int blinkInterval = 100)
        {
            SendCommand("444444");
        }

        public void Disco(double intervalInSeconds)
        {
            SendCommand("FF00AA");
        }

        public LightColour CurrentColour
        {
            get { return LightColour.Blue; }
        }
    }
}
