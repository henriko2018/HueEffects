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
        public IEnumerable<SunPhase> SunPhases { get; set; }
    }

    public interface IEffectConfig
    {
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
            TurnOnAt = new SunsetConfig();
            TurnOffAt = new SunriseConfig();
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

    public class TimeConfig
    {
        public TimeConfig()
        {
            // Default values
            Type = TimeType.Sun;
            RandomInterval = 0;
            TransitionTime = TimeSpan.FromHours(1);
        }

        public TimeType Type { get; set; } // Fixed or sun

        public string SunPhaseName { get; set; }

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

        public Location Location { get; set; }

        private SunPhase SunPhase
        {
            get
            {
                if (Location == null)
                    throw new ArgumentNullException(nameof(Location), "Location not set when trying to get SunPhase");
                var sunPhase = GetSunPhases(Location).SingleOrDefault(sp => sp.Name.Value == SunPhaseName);
                // For some reason, sunPhase == default(SunPhase) throws System.NullReferenceException.
                if (sunPhase.Name == null)
                    throw new ArgumentOutOfRangeException(nameof(SunPhaseName), $"Unknown sun phase {SunPhaseName}");
                return sunPhase;
            }
        }

        public static IEnumerable<SunPhase> GetSunPhases(Location location)
        {
            var sunPhases = SunCalc.GetSunPhases(DateTime.Now, location.Lat, location.Long).ToList();
            return sunPhases.Select(sp => new SunPhase(sp.Name, sp.PhaseTime.ToLocalTime())).OrderBy(sp => sp.PhaseTime);
        }

        private static readonly Random Random = new Random();
        
        /// <summary>
        /// Returns number of milliseconds until configured time adjusted with random interval. If it has passed, tomorrow's value is used.
        /// </summary>
        public double GetUntil(bool adjustForTransition)
        {
            var time = Type == TimeType.Fixed ? FixedTime : SunPhase.PhaseTime;
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

    public class SunriseConfig : TimeConfig
    {
        public SunriseConfig()
        {
            // Default values
            FixedTime = DateTime.Today + new TimeSpan(0, 6, 0, 0);
            SunPhaseName = SunCalcNet.Model.SunPhaseName.Sunrise.Value;
        }
    }

    public class SunsetConfig : TimeConfig
    {
        public SunsetConfig()
        {
            // Default values
            FixedTime = DateTime.Today + new TimeSpan(0, 21, 0, 0);
            SunPhaseName = SunCalcNet.Model.SunPhaseName.Sunset.Value;
        }
    }

    public enum TimeType { Fixed, Sun }
}
