using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace ProjectEarthServerAPI.Models
{
	public class ProfileResponse
	{
		public static Dictionary<string, ProfileLevel> levels { get; private set; }

		// Setup the level distribution dict when the class is first called
		static ProfileResponse()
		{
			levels = new Dictionary<string, ProfileLevel>();
			levels.Add("2", new ProfileLevel {experienceRequired = 500, rewards = new Rewards {Rubies = 15, Inventory = new RewardComponent[] {new RewardComponent {Id = new Guid("730573d1-ba59-4fd4-89e0-85d4647466c2"), Amount = 1}, new RewardComponent {Id = new Guid("20dbd5fc-06b7-1aa1-5943-7ddaa2061e6a"), Amount = 8}, new RewardComponent {Id = new Guid("1eaa0d8c-2d89-2b84-aa1f-b75ccc85faff"), Amount = 64}}}});
			levels.Add("3", new ProfileLevel {experienceRequired = 1500});
			levels.Add("4", new ProfileLevel {experienceRequired = 2800});
			levels.Add("5", new ProfileLevel {experienceRequired = 4600});
			levels.Add("6", new ProfileLevel {experienceRequired = 6100});
			levels.Add("7", new ProfileLevel {experienceRequired = 7800});
			levels.Add("8", new ProfileLevel {experienceRequired = 10100});
			levels.Add("9", new ProfileLevel {experienceRequired = 13300});
			levels.Add("10", new ProfileLevel {experienceRequired = 17800});
			levels.Add("11", new ProfileLevel {experienceRequired = 21400});
			levels.Add("12", new ProfileLevel {experienceRequired = 25700});
			levels.Add("13", new ProfileLevel {experienceRequired = 31300});
			levels.Add("14", new ProfileLevel {experienceRequired = 39100});
			levels.Add("15", new ProfileLevel {experienceRequired = 50000});
			levels.Add("16", new ProfileLevel {experienceRequired = 58700});
			levels.Add("17", new ProfileLevel {experienceRequired = 68700});
			levels.Add("18", new ProfileLevel {experienceRequired = 82700});
			levels.Add("19", new ProfileLevel {experienceRequired = 101700});
			levels.Add("20", new ProfileLevel {experienceRequired = 128700});
			levels.Add("21", new ProfileLevel {experienceRequired = 137400});
			levels.Add("22", new ProfileLevel {experienceRequired = 147000});
			levels.Add("23", new ProfileLevel {experienceRequired = 157000});
			levels.Add("24", new ProfileLevel {experienceRequired = 169000});
			levels.Add("25", new ProfileLevel {experienceRequired = 185000});
		}

		public ProfileResult result { get; set; }
		public object continuationToken { get; set; }
		public object expiration { get; set; }
		public Updates updates { get; set; }

		public ProfileResponse(ProfileData profileData)
		{
			result = ProfileResult.of(profileData);
		}
	}

	public class ProfileResult : ProfileData
	{
		public Dictionary<string, ProfileLevel> levelDistribution { get; set; }

		public static ProfileResult of(ProfileData profileData)
		{
			return new ProfileResult
			{
				totalExperience = profileData.totalExperience,
				level = profileData.level,
				health = profileData.health,
				healthPercentage = profileData.healthPercentage,
				levelDistribution = ProfileResponse.levels
			};
		}
	}

	public class ProfileLevel
	{
		public int experienceRequired { get; set; }
		public Rewards rewards { get; set; }

		public ProfileLevel()
		{
			rewards = new Rewards();
		}
	}

	public class ProfileData
	{
		public int totalExperience { get; set; }
		public int level { get; set; }

		public int currentLevelExperience
		{
			get
			{
				ProfileLevel profileLevel;
				if (ProfileResponse.levels.TryGetValue(level.ToString(), out profileLevel))
				{
					return totalExperience - profileLevel.experienceRequired;
				}

				return totalExperience;
			}
		}

		public int experienceRemaining
		{
			get
			{
				ProfileLevel profileLevel;
				if (ProfileResponse.levels.TryGetValue((level + 1).ToString(), out profileLevel))
				{
					return profileLevel.experienceRequired - currentLevelExperience;
				}

				return 0;
			}
		}

		public int? health { get; set; }
		public float healthPercentage { get; set; }

		public ProfileData()
		{
			totalExperience = 0;
			level = 1;
			health = 20;
			healthPercentage = 100f;
		}
	}
}
