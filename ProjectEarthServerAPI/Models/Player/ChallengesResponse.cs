using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using ProjectEarthServerAPI.Models.Features;
using ProjectEarthServerAPI.Util;

namespace ProjectEarthServerAPI.Models.Player
{
	public class ChallengesResponse
	{
		public ChallengesList result { get; set; }
		public Updates updates { get; set; }

		public static ChallengesResponse FromFile(string path)
		{
			var jsontext = File.ReadAllText(path);
			return JsonConvert.DeserializeObject<ChallengesResponse>(jsontext);
		}

		public ChallengesResponse()
		{
			if (StateSingleton.Instance.challengeStorage != null)
			{
				result = new ChallengesList
				{
					activeSeasonChallenge = StateSingleton.Instance.challengeStorage.activeSeasonChallenge,
					challenges = StateSingleton.Instance.challengeStorage.challenges
						.Where(pred => pred.Value.challengeBackendInformation.isDefaultChallenge)
						.ToDictionary(pred => pred.Key,
							pred => pred.Value.challengeInfo)
				};
			}
		}
	}

	public class ChallengesList
	{
		public Dictionary<Guid, ChallengeInfo> challenges { get; set; }
		public Guid? activeSeasonChallenge { get; set; }
	}

	public class ChallengeStorage
	{
		public Dictionary<Guid, ChallengeBackend> challenges { get; set; }
		public Guid activeSeasonChallenge { get; set; }

		public static ChallengeStorage FromFiles(string challengesFolderLocation)
		{
			ChallengeStorage storage = new();
			storage.challenges = new();
			storage.activeSeasonChallenge = StateSingleton.Instance.config.activeSeasonChallenge;

			foreach (string filePath in Directory.EnumerateFiles(challengesFolderLocation, "*.json"))
			{
				var text = File.ReadAllText(filePath);
				var challenge = JsonConvert.DeserializeObject<ChallengeBackend>(text);
				var challengeId = Guid.Parse(Path.GetFileNameWithoutExtension(filePath));
				storage.challenges.Add(challengeId, challenge);
			}

			return storage;
		}
	}

	public class ChallengeBackend
    {
		public ChallengeBackendInformation challengeBackendInformation { get; set; }
		public ChallengeInfo challengeInfo { get; set; }
		public ChallengeRequirements challengeRequirements { get; set; } // TODO: Change into list, and add needed stuff for multiple requirements.
    }

	public class ChallengeBackendInformation {
		public bool isDefaultChallenge { get; set; }
		public string readableName { get; set; }
		public bool progressWhenLocked { get; set; }
	}

    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
	public class ChallengeRequirements
    {
		public List<ItemChallenge> items { get; set; }
		public List<TappableChallenge> tappables { get; set; }
		public List<ChallengeChallenge> challenges { get; set; }
		//public List<MobChallenge> mobs { get; set; }
		//public List<MultiplayerChallenge> multiplayer { get; set; }
    }

	public class ItemChallenge
	{
		public ChallengeItems targetItems { get; set; } // Event Id items, like "Collect x cobblestone" = Guid of cobblestone
		public ChallengeItems sourceItems { get; set; } // For conversions, might be removable depending on how we do multiplayer stuff
		[JsonProperty(ItemConverterType = typeof(StringEnumConverter))]
		public List<ItemEventAction> action { get; set; } // What the challenge is, like award/craft/smelt item
		[JsonProperty(ItemConverterType = typeof(StringEnumConverter))]
		public List<EventLocation>? location { get; set; } // For tappable/challenge challenges, like "Get x wood from tappables" = tappable
		public int threshold { get; set; } // Amount of that requirement in challenge, like "Collect 2 cobblestone" = 2
		public bool shouldBeUnique { get; set; } // If all items should only be counted once, for stuff that matches tags. TODO: Implement
	}

	public class TappableChallenge
	{
		public List<string> targetTappableTypes { get; set; }
		public int threshold { get; set; }
		public bool shouldBeUnique { get; set; }
	}

	public class ChallengeChallenge
	{
		public List<Guid> targetChallengeIds { get; set; } // Challenge ids to complete
		[JsonProperty(ItemConverterType = typeof(StringEnumConverter))]
		public List<ChallengeDuration> durations { get; set; }
		public List<string> rarities { get; set; }
		public int threshold { get; set; }
	}

	public class ChallengeItems {
        public List<Guid> itemIds { get; set; }
        public List<string> tags { get; set; }
        [JsonProperty(ItemConverterType = typeof(StringEnumConverter))]
		public List<Item.Rarity> rarity { get; set; }
    }

	public class ChallengeInfo
    {
	    public Guid referenceId { get; set; }
	    [JsonConverter(typeof(StringEnumConverter))]
	    public ChallengeDuration duration { get; set; }
	    [JsonConverter(typeof(StringEnumConverter))]
	    public ChallengeType type { get; set; }
		[JsonConverter(typeof(DateTimeConverter)), JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
	    public DateTime? endTimeUtc { get; set; }
	    public Rewards rewards { get; set; }
	    public int percentComplete { get; set; }
	    public bool isComplete { get; set; }
	    [JsonConverter(typeof(StringEnumConverter))]
	    public ChallengeState state { get; set; }
	    [JsonConverter(typeof(StringEnumConverter))]
	    public ChallengeCategory category { get; set; }
	    public int currentCount { get; set; }
	    public int totalThreshold { get; set; }
	    public Guid? parentId { get; set; }
	    public int order { get; set; }
	    public string rarity { get; set; }
	    [JsonConverter(typeof(StringEnumConverter))]
	    public ChallengeLogicCondition prerequisiteLogicalCondition { get; set; }
	    public List<Guid> prerequisiteIds { get; set; }
	    public Guid? groupId { get; set; }
	    public ChallengeProperties clientProperties { get; set; }
    }

	[JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
	public class ChallengeProperties
	{
		public string topDecorationTexture { get; set; }
		public string challengeCollectedAudioEvent { get; set; }
		public string challengeSelectedAudioEvent { get; set; }
		public string frameTexture { get; set; }
	}

	public enum ChallengeDuration
	{
		Career,
		Season,
		SignIn,
		OOBE,
		PersonalTimed,
		PersonalContinuous
	}

	public enum ChallengeType
	{
		Journal,
		Regular
	}

	public enum ChallengeState
	{
		Active,
		Completed,
		Locked
	}

	public enum ChallengeCategory
	{
		Building,
		Collection,
		cow,
		chicken,
		oobe,
		pig,
		rabbit,
		retention,
		season_20,
		season_19,
		season_18,
		season_17,
		season_16,
		season_10,
		season_9,
		season_8,
		season_7,
		season_6,
		season_5,
		season_4,
		season_3,
		season_2,
		season_1,
		sheep,
		Smelting
	}

	public enum ChallengeLogicCondition
	{
		And,
		Or
	}
}
