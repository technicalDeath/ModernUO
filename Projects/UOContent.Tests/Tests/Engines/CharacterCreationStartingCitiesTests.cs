using System.Linq;
using Server;
using Server.Engines.CharacterCreation;
using Xunit;

namespace UOContent.Tests;

[Collection("Sequential UOContent Tests")]
public class CharacterCreationStartingCitiesTests
{
    private static void With(System.Func<CityInfo[], CityInfo[]> filter, System.Action test)
    {
        var previous = CharacterCreation.StartingCitiesFilter;

        try
        {
            CharacterCreation.StartingCitiesFilter = filter;
            test();
        }
        finally
        {
            CharacterCreation.StartingCitiesFilter = previous;
        }
    }

    [Fact]
    public void WithNoFilterEveryStartingCityIsOffered()
    {
        With(null, () => Assert.Equal(CharacterCreation.GetStartingCities().Length, CharacterCreation.GetStartingCities().Distinct().Count()));
    }

    [Fact]
    public void AFilterNarrowsTheListThatIsSentAndIndexed()
    {
        With(
            all => all.Take(1).ToArray(),
            () =>
            {
                Assert.Single(CharacterCreation.GetStartingCities());
            }
        );
    }

    [Fact]
    public void AFilterThatReturnsNullLeavesTheListAlone()
    {
        var before = CharacterCreation.GetStartingCities().Length;

        With(_ => null, () => Assert.Equal(before, CharacterCreation.GetStartingCities().Length));
    }
}
