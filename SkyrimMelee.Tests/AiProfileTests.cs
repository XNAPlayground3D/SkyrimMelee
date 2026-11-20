using SkyrimMelee.Ai;

namespace SkyrimMelee.Tests;

public class AiProfileTests
{
	[Fact]
	public void ThreePresetsAreAvailable()
	{
		Assert.Equal(3, AiProfile.All.Count);
		Assert.Equal("Novice", AiProfile.All[0].Name);
		Assert.Equal("Adept", AiProfile.All[1].Name);
		Assert.Equal("Master", AiProfile.All[2].Name);
	}

	[Theory]
	[InlineData("novice", "Novice")]
	[InlineData("ADEPT", "Adept")]
	[InlineData("Master", "Master")]
	public void ByNameIsCaseInsensitive(string input, string expected)
	{
		Assert.Equal(expected, AiProfile.ByName(input)?.Name);
	}

	[Fact]
	public void UnknownProfileIsNull()
	{
		Assert.Null(AiProfile.ByName("Dragonborn"));
	}

	[Fact]
	public void MasterReactsAndBlocksBetterThanNovice()
	{
		Assert.True(AiProfile.Master.ReactionSeconds < AiProfile.Novice.ReactionSeconds);
		Assert.True(AiProfile.Master.BlockChance > AiProfile.Novice.BlockChance);
		Assert.True(AiProfile.Master.MistakeChance < AiProfile.Novice.MistakeChance);
	}
}
