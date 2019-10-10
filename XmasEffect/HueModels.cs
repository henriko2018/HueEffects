using System.Collections.Generic;

namespace XmasEffect
{
    public class Bridge
    {
        public string Id { get; set; }
        public string InternalIpAddress { get; set; }
        public string MacAddress { get; set; }
        public string Name { get; set; }
    }

    public class LightGroup
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public IEnumerable<string> LightIds { get; set; }
    }

    public class Light
    {
        public string Id { get; set; }
        public State State { get; set; }
        public string Type { get; set; }
        public string Name { get; set; }
        public string ModelId { get; set; }
        public string ManufacturerName { get; set; }
        public string ProductName { get; set; }
        public Capabilities Capabilities { get; set; }
        public string UniqueId { get; set; }
        public string SwVersion { get; set; }
        public string SwConfigId { get; set; }
        public string ProductId { get; set; }
    }

    public class Capabilities
    {
        public bool Certified { get; set; }
        public Control Control { get; set; }
    }

    public class Control
    {
        public int MinDimLevel { get; set; }
        public int MaxLumen { get; set; }
        public string ColorGamutType { get; set; }
        public float[][] ColorGamut { get; set; }
        public Ct Ct { get; set; }
    }

    public class Ct
    {
        public int Min { get; set; }
        public int Max { get; set; }
    }

    public class State
    {
        public bool On { get; set; }
        public int Bri { get; set; }
        public int Hue { get; set; }
        public int Sat { get; set; }
        public string Effect { get; set; }
        public float[] Xy { get; set; }
        public int Ct { get; set; }
        public string Alert { get; set; }
        public string ColorMode { get; set; }
        public string Mode { get; set; }
        public bool Reachable { get; set; }
    }
}
