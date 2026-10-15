namespace CityForge.UI
{
    /// <summary>The historical stages available when founding a region.</summary>
    public static class CityForgeTimeline
    {
        public sealed class Period
        {
            public readonly string Name;
            public readonly int FirstYear;
            public readonly int LastYear;

            public Period(string name, int firstYear, int lastYear)
            {
                Name = name;
                FirstYear = firstYear;
                LastYear = lastYear;
            }
        }

        public sealed class Era
        {
            public readonly string Name;
            public readonly Period[] Periods;

            public int FirstYear => Periods[0].FirstYear;
            public int LastYear => Periods[Periods.Length - 1].LastYear;

            public Era(string name, params Period[] periods)
            {
                Name = name;
                Periods = periods;
            }
        }

        public static readonly Era[] Eras =
        {
            new Era("Founder's Era",
                new Period("Frontier Settlement", 1740, 1759),
                new Period("Colonial Growth", 1760, 1779),
                new Period("A New Nation", 1780, 1799),
                new Period("Early Republic", 1800, 1819)),
            new Era("Industrial Era",
                new Period("Transportation Revolution", 1820, 1839),
                new Period("Industrial Expansion", 1840, 1859),
                new Period("Transformation", 1860, 1869)),
            new Era("Victorian Era",
                new Period("Railroad City", 1870, 1879),
                new Period("Urban Boom", 1880, 1889),
                new Period("Metropolitan Age", 1890, 1899)),
            new Era("Machine Age",
                new Period("New Century", 1900, 1909),
                new Period("Motor Age Begins", 1910, 1920)),
            new Era("Modern Era",
                new Period("Roaring Twenties", 1921, 1929),
                new Period("Depression Era", 1930, 1938),
                new Period("Wartime City", 1939, 1945)),
            new Era("Postwar Era",
                new Period("Postwar Boom", 1946, 1954),
                new Period("Suburban Expansion", 1955, 1964),
                new Period("Urban Transformation", 1965, 1974)),
            new Era("Contemporary Era",
                new Period("Reinvention", 1975, 1989),
                new Period("Information Age", 1990, 2000),
                new Period("New Millennium", 2001, 2009),
                new Period("Connected City", 2010, 2019),
                new Period("Twenty-First Century City", 2020, 2026))
        };
    }
}
