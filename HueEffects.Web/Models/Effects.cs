using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Q42.HueApi.Models.Groups;

namespace HueEffects.Web.Models
{
    public class Effects
    {
        public XmasEffect XmasEffect { get; set; }
        public WarmupEffect WarmupEffect { get; set; }
        public IEnumerable<Group> LightGroups { get; set; }
    }

    public interface IEffect
    {
        bool Active { get; set; }
        string LightGroup { get; set; }
    }

    public class XmasEffect : IEffect
    {
        public bool Active { get; set; }
        public string LightGroup { get; set; }

        /// <summary>
        /// Length of color cycle in seconds.
        /// </summary>
        public int CycleLength { get; set; }
    }

    public class WarmupEffect : IEffect
    {
        public bool Active { get; set; }
        public string LightGroup { get; set; }
        public DateTime TurnOnAt { get; set; }
        public TimeSpan WarmUp { get; set; }
        public TimeSpan CoolDown { get; set; }
        public DateTime TurnOffAt { get; set; }

        public const int MinTemp = 153;
        public const int MaxTemp = 500; // TODO: Check these values

        [Range(MinTemp, MaxTemp)] 
        public int UseMinTemp { get; set; }

        [Range(MinTemp, MaxTemp)]
        public int UseMaxTemp { get; set; }
    }
}
