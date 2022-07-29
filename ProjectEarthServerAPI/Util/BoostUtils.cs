using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using ProjectEarthServerAPI.Models;
using ProjectEarthServerAPI.Models.Features;

namespace ProjectEarthServerAPI.Util
{
	public class BoostUtils
	{
		private static CatalogResponse catalog = StateSingleton.Instance.catalog;

		public static BoostResponse ReadBoosts(string playerId)
		{
			var resp = GenericUtils.ParseJsonFile<BoostResponse>(playerId, "boosts");
			if (resp.result.miniFigs.Count != 5)
			{
				resp.result.miniFigs = new List<object>(new object[5]);
				WriteBoosts(playerId, resp);
			}

			if (resp.result.potions.Count != 5)
			{
				resp.result.potions = new List<Potion>(new Potion[5]);
				WriteBoosts(playerId, resp);
			}

			return resp;
		}

		public static bool WriteBoosts(string playerId, BoostResponse resp)
		{
			return GenericUtils.WriteJsonFile(playerId, resp, "boosts");
		}

		public static CraftingUpdates ActivateBoost(string playerId, Guid boostId)
		{
			var updates = new CraftingUpdates {updates = new Updates()};

			List<DateTime> expirationTimes = new List<DateTime>(); // This looks bad (and it totally is), but we can optimize it at a later date
			var currentTime = DateTime.UtcNow;

			var baseBoosts = ReadBoosts(playerId);
			var boostToApply = catalog.result.items.Find(match => match.id == boostId);


			var indexNum = 0;
			if (baseBoosts.result.scenarioBoosts.death != null)
				indexNum = baseBoosts.result.scenarioBoosts.death.Count; // Only used if boost is scenario specific

			var nextStreamId = GenericUtils.GetNextStreamVersion();

			var locOfEffect = baseBoosts.result.activeEffects.Find(match => match.effect == boostToApply.item.boostMetadata.effects[0]); // null when not an active effect, e.g scenario boost

			ActiveBoost locOfBoost = null;


			if (baseBoosts.result.scenarioBoosts.death != null)
			{
				locOfBoost = baseBoosts.result.scenarioBoosts.death.Find(match =>
					match.effects.Any(pred => pred == boostToApply.item.boostMetadata.effects[0]));
			}

			if (locOfEffect != null && boostToApply.item.boostMetadata.additive && boostToApply.item.boostMetadata.scenario == null)
			{
				locOfEffect.effect.duration += boostToApply.item.boostMetadata.activeDuration.Value.TimeOfDay;
				baseBoosts.result.potions.Find(match => match.itemId == boostId).expiration +=
					boostToApply.item.boostMetadata.activeDuration.Value.TimeOfDay;
			}
			else if (locOfBoost != null && boostToApply.item.boostMetadata.additive && boostToApply.item.boostMetadata.scenario == "Death")
			{
				indexNum = baseBoosts.result.scenarioBoosts.death.IndexOf(locOfBoost);
				locOfBoost.expiration += boostToApply.item.boostMetadata.activeDuration.Value.TimeOfDay;
				baseBoosts.result.potions.Find(match => match.itemId == boostId).expiration += boostToApply.item.boostMetadata.activeDuration.Value.TimeOfDay;
			}
			else
			{
				Dictionary<Guid, string> uuidDict = new Dictionary<Guid, string>();
				if (boostToApply.item.boostMetadata.scenario == null)
				{
					foreach (Item.Effect effect in boostToApply.item.boostMetadata.effects)
					{
						baseBoosts.result.activeEffects.Add(new ActiveEffect {effect = effect, expiration = currentTime.Add(effect.duration.Value)});
					}
				}
				else
				{
					baseBoosts.result.scenarioBoosts.death ??= new List<ActiveBoost>();

					baseBoosts.result.scenarioBoosts.death.Add(new ActiveBoost {effects = new List<Item.Effect>(), enabled = true, expiration = currentTime.Add(boostToApply.item.boostMetadata.activeDuration.Value.TimeOfDay), instanceId = Guid.NewGuid().ToString()});

					foreach (Item.Effect effect in boostToApply.item.boostMetadata.effects)
					{
						baseBoosts.result.scenarioBoosts.death[indexNum].effects.Add(effect);
					}

					uuidDict.Add(boostId, baseBoosts.result.scenarioBoosts.death[indexNum].instanceId);
				}

				Potion potion = null;

				if (boostToApply.item.boostMetadata.activeDuration != null)
				{
					potion = new Potion {enabled = true, expiration = currentTime.Add(boostToApply.item.boostMetadata.activeDuration.Value.TimeOfDay), instanceId = Guid.NewGuid().ToString(), itemId = boostId};
				}
				else if (boostToApply.item.boostMetadata.effects[0].duration != null)
				{
					potion = new Potion {enabled = true, expiration = currentTime.Add(boostToApply.item.boostMetadata.effects[0].duration.Value), instanceId = Guid.NewGuid().ToString(), itemId = boostId};
				}

				if (uuidDict.ContainsKey(boostId) && potion != null)
				{
					potion.instanceId = uuidDict[boostId];
				}

				var nullPotionIndex = baseBoosts.result.potions.FindIndex(match => match == null);
				if (nullPotionIndex != -1)
					baseBoosts.result.potions[nullPotionIndex] = potion;
			}


			if (boostToApply.item.boostMetadata.canBeRemoved)
			{
				InventoryUtils.RemoveItemFromInv(playerId, boostId); // UNCOMMENT/COMMENT THIS LINE TO REMOVE BOOSTS FROM INVENTORY WHEN USED
				updates.updates.inventory = nextStreamId;
			}

			foreach (ActiveEffect effect in baseBoosts.result.activeEffects)
			{
				expirationTimes.Add(effect.expiration.Value);
			}

			if (baseBoosts.result.scenarioBoosts.death != null)
				foreach (ActiveBoost boost in baseBoosts.result.scenarioBoosts.death)
				{
					expirationTimes.Add(boost.expiration.Value);
				}

			baseBoosts.result.expiration = expirationTimes.Min();

			baseBoosts.result.statusEffects = CalculateStatusEffects(baseBoosts);

			WriteBoosts(playerId, baseBoosts);
			updates.updates.boosts = nextStreamId;
			return updates;
		}

		public static BoostResponse UpdateBoosts(string playerId)
		{
			List<DateTime> expirationTimes = new List<DateTime>(); // This looks bad (and it totally is), but we can optimize it at a later date
			var baseBoosts = ReadBoosts(playerId);
			var currentTime = DateTime.UtcNow;

			baseBoosts.result.activeEffects.RemoveAll(match => match.expiration < currentTime);

			baseBoosts.result.scenarioBoosts.death?.RemoveAll(match => match.expiration < currentTime);

			baseBoosts.result.potions.RemoveAll(match => match?.expiration < currentTime);

			foreach (ActiveEffect effect in baseBoosts.result.activeEffects)
			{
				expirationTimes.Add(effect.expiration.Value);
			}

			if (baseBoosts.result.scenarioBoosts.death != null)
				foreach (ActiveBoost boost in baseBoosts.result.scenarioBoosts.death)
				{
					expirationTimes.Add(boost.expiration.Value);
				}

			try
			{
				baseBoosts.result.expiration = expirationTimes.Min();
			}
			catch
			{
				baseBoosts.result.expiration = null;
			}

			baseBoosts.result.statusEffects = CalculateStatusEffects(baseBoosts);


			WriteBoosts(playerId, baseBoosts);

			return baseBoosts;
		}

		public static CraftingUpdates RemoveBoost(string playerId, string boostInstanceId)
		{
			var nextStreamId = GenericUtils.GetNextStreamVersion();
			var baseBoosts = ReadBoosts(playerId);
			var potionToRemove = baseBoosts.result.potions.Find(match => match.instanceId == boostInstanceId);
			var boostToRemove = catalog.result.items.Find(match => match.id == potionToRemove.itemId);

			baseBoosts.result.scenarioBoosts.death?.RemoveAll(match => match.instanceId == boostInstanceId);

			baseBoosts.result.potions.Remove(potionToRemove);
			baseBoosts.result.activeEffects.RemoveAll(match =>
				//boostToRemove.item.boostMetadata.effects.Contains(match.effect) && //returns false
				match.expiration == potionToRemove.expiration);

			baseBoosts.result.statusEffects = CalculateStatusEffects(baseBoosts);

			WriteBoosts(playerId, baseBoosts);

			var updates = new CraftingUpdates {updates = new Updates()};

			updates.updates.boosts = nextStreamId;

			return updates;
		}

		public static StatusEffects CalculateStatusEffects(BoostResponse boosts) // TODO: Which values does it use and need? More research on official servers needed
		{
			var baseBoosts = boosts;
			var statusEffects = new StatusEffects();
			var baseSettings = StateSingleton.Instance.settings;

			statusEffects.tappableInteractionRadius =
				(int?)(baseSettings.result.tappableinteractionradius + baseBoosts.result.activeEffects.Find(match => match.effect.type.ToLower() == "tappableinteractionradius")?.effect.value);

			//baseBoosts.result.statusEffects.itemExperiencePointRates = baseBoosts.result.activeEffects.Find(match => match.effect.type.ToLower() == "tappableinteractionradius").effect.value);
			statusEffects.attackDamageRate = (int?)baseBoosts.result.activeEffects.Find(match => match.effect.type.ToLower() == "tappableinteractionradius")?.effect.value;
			statusEffects.blockDamageRate = (int?)baseBoosts.result.activeEffects.Find(match => match.effect.type.ToLower() == "blockdamage")?.effect.value;
			statusEffects.craftingSpeed = (int?)baseBoosts.result.activeEffects.Find(match => match.effect.type.ToLower() == "craftingspeed")?.effect.value;
			statusEffects.experiencePointRate = (int?)baseBoosts.result.activeEffects.Find(match => match.effect.type.ToLower() == "itemexperiencepoints")?.effect.value; // Find out if this or the other one
			statusEffects.foodHealthRate = (int?)baseBoosts.result.activeEffects.Find(match => match.effect.type.ToLower() == "foodhealth")?.effect.value;
			statusEffects.maximumPlayerHealth = (int?)baseBoosts.result.activeEffects.Find(match => match.effect.type.ToLower() == "maximumplayerhealth")?.effect.value+20;
			statusEffects.playerDefenseRate = (int?)baseBoosts.result.activeEffects.Find(match => match.effect.type.ToLower() == "playerdefense")?.effect.value;
			statusEffects.smeltingFuelIntensity = (int?)baseBoosts.result.activeEffects.Find(match => match.effect.type.ToLower() == "smeltingfuelintensity")?.effect.value;

			if (statusEffects.maximumPlayerHealth == null) {
				statusEffects.maximumPlayerHealth = 20;
			}

			return statusEffects;
		}
	}
}
