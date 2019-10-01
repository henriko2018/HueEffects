using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Q42.HueApi.Models.Groups;
using SunCalcNet;
using SunCalcNet.Model;

namespace HueEffects.Web.Models
{
    public class EffectsConfig
    {
        public XmasEffectConfig XmasEffectConfig { get; set; }
        public WarmupEffectConfig WarmupEffectConfig { get; set; }
        public IEnumerable<Group> LightGroups { get; set; }
    }

    public interface IEffectConfig
    {
        bool Active { get; set; }
        string LightGroup { get; set; }
    }

    public class XmasEffectConfig : IEffectConfig
    {
        public XmasEffectConfig()
        {
            // Default values
            Active = false;
            CycleLength = 60;
        }

        public bool Active { get; set; }
        public string LightGroup { get; set; }

        /// <summary>
        /// Length of color cycle in seconds.
        /// </summary>
        [Range(2, int.MaxValue)]
        public int CycleLength { get; set; }
    }

    public class WarmupEffectConfig : IEffectConfig
    {
        public const int MinTemp = 153;
        public const int MaxTemp = 454;

        public WarmupEffectConfig()
        {
            // Default values
            Active = false;
            TurnOnAt = new SunSetConfig();
            TurnOffAt = new SunRiseConfig();
            UseMinTemp = MinTemp;
            UseMaxTemp = MaxTemp;
        }

        public bool Active { get; set; }
        public string LightGroup { get; set; }
        public TimeConfig TurnOnAt { get; set; }
        public TimeConfig TurnOffAt { get; set; }
        [Range(MinTemp, MaxTemp)] 
        public int UseMinTemp { get; set; }
        [Range(MinTemp, MaxTemp)]
        public int UseMaxTemp { get; set; }
    }

    public class SunRiseConfig : TimeConfig
    {
        public SunRiseConfig()
        {
            // Default values
            FixedTime = DateTime.Today + new TimeSpan(0, 6, 0, 0);
            SunTime = GetSunPhases().sunRise;
        }
    }

    public class SunSetConfig : TimeConfig
    {
        public SunSetConfig()
        {
            // Default values
            FixedTime = DateTime.Today + new TimeSpan(0, 21, 0, 0);
            SunTime = GetSunPhases().sunSet;
        }
    }

    public class TimeConfig
    {
        public TimeConfig()
        {
            // Default values
            Type = TimeType.Sun;
            RandomInterval = 0;
            TransitionTime = TimeSpan.FromHours(1);
        }

        public TimeType Type { get; set; }

        /// <summary>
        /// Read-only. Only the time portion is used.
        /// </summary>
        [DataType(DataType.Time)]
        public DateTime SunTime { get; set; }

        /// <summary>
        /// Only the time portion is used.
        /// </summary>
        [DataType(DataType.Time)]
        public DateTime FixedTime { get; set; }

        /// <summary>
        /// In minutes.
        /// </summary>
        public int RandomInterval { get; set; }

        /// <summary>
        /// Warm-up and cool-down
        /// </summary>
        public TimeSpan TransitionTime { get; set; }

        protected static (DateTime sunSet, DateTime sunRise) GetSunPhases()
        {
            var sunPhases = SunCalc.GetSunPhases(DateTime.Now, 59.4664329, 18.0842061).ToList(); // TODO: Remove hard-coding
            var sunSet = sunPhases.Single(sp => sp.Name.Value == SunPhaseName.Sunset.Value).PhaseTime.ToLocalTime();
            var sunRise = sunPhases.Single(sp => sp.Name.Value == SunPhaseName.Sunrise.Value).PhaseTime.ToLocalTime();
            return (sunSet, sunRise);
        }

        private static readonly Random Random = new Random();
        /// <summary>
        /// Returns number of milliseconds until configured time adjusted with random interval. If it has passed, tomorrow's value is used.
        /// </summary>
        public double GetUntil(bool adjustForTransition)
        {
            var time = Type == TimeType.Fixed ? FixedTime : SunTime;
            if (adjustForTransition)
                time -= TransitionTime;
            var maxAdjustment = RandomInterval * 60 * 1000; // Now in ms
            var adjustment = Random.Next(-maxAdjustment, maxAdjustment);
            time += new TimeSpan(0, 0, 0, 0, adjustment);
            if (time <= DateTime.Now)
                time += new TimeSpan(1, 0, 0, 0); // Small error here: The sun rises and sets on a different slightly time tomorrow.
            return (time - DateTime.Now).TotalMilliseconds;
        }

        public static IEnumerable<TimeType> TimeTypes => Enum.GetValues(typeof(TimeType)).Cast<TimeType>();
    }

    public enum TimeType { Fixed, Sun }
}
