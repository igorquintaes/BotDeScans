using Bogus;
using Remora.Rest.Core;
using System.Collections.Generic;

namespace BotDeScans.UnitTests.Extensions
{
    public static class FakerExtensions
    {
        public static Snowflake Snowflake(this Randomizer randomizer) =>
            new (randomizer.ULong());

        public static IEnumerable<Snowflake> Snowflake(this Randomizer randomizer, int quantity)
        {
            for (int i = 0; i < quantity; i++)
                yield return new (randomizer.ULong());
        }
    }
}
