namespace HueEffects.Web
{
    public class Options
    {
        public Location Location { get; set; }
        public string AppKey { get; set; }
    }

    public class Location
    {
        public double Lat { get; set; }
        public double Long { get; set; }
    }
}
