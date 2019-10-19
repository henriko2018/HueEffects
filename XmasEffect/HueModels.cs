using System.Collections.Generic;

namespace XmasEffect
{
    public class Bridge
    {
        public string InternalIpAddress { get; set; }
    }

    public class LightGroup
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public IEnumerable<string> Lights { get; set; }
    }

    public class Light
    {
        public string Id { get; set; }
        public State State { get; set; }
        public Capabilities Capabilities { get; set; }
    }

    public class Capabilities
    {
        public Control Control { get; set; }
    }

    public class Control
    {
        public float[][] ColorGamut { get; set; }
        public Ct Ct { get; set; }
    }

    public class Ct
    {
    }

    public class State
    {
        public bool On { get; set; }
        public int Sat { get; set; }
        public float[] Xy { get; set; }
        public int Ct { get; set; }
    }
}
