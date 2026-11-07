using System;
using CityForge.Lots;

namespace CityForge.UI
{
    /// <summary>The historical stages available when founding a region.</summary>
    public static class CityForgeTimeline
    {
        [Serializable]
        public sealed class Period
        {
            public string name;
            public int firstYear;
            public int lastYear;

            public string Name => name;
            public int FirstYear => firstYear;
            public int LastYear => lastYear;
        }

        [Serializable]
        public sealed class Era
        {
            public string id;
            public string name;
            public Period[] periods;

            public string Name => name;
            public Period[] Periods => periods;
            public int FirstYear => periods[0].FirstYear;
            public int LastYear => periods[periods.Length - 1].LastYear;
        }

        public static Era[] Eras => LotContentCatalog.Current.eras;
        public static Era Find(string id) => Array.Find(Eras, era => era.id == id);
    }
}
