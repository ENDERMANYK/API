using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.OpenApi.Extensions;
using ProjectEarthServerAPI.Models;
using ProjectEarthServerAPI.Models.Player;
using Serilog;

namespace ProjectEarthServerAPI.Util
{
	public class ChallengeUtils
	{

		private static Random random = new Random();

		public static bool ActivateChallengeForPlayer(string playerId, Guid challengeId)
		{
			var challenge = StateSingleton.Instance.challengeStorage.challenges[challengeId].challengeInfo;
			var playerChallenges = ReadChallenges(playerId);
			bool shouldBeActivated = false;

			foreach (KeyValuePair<Guid, ChallengeInfo> prereqChallenge in playerChallenges.result.challenges.Where(pred =>
				challenge.prerequisiteIds.Contains(pred.Key)))
			{
				if (!shouldBeActivated)
				{
					switch (challenge.prerequisiteLogicalCondition)
					{
						case ChallengeLogicCondition.And:
							if (!prereqChallenge.Value.isComplete)
								return false;
							break;

						case ChallengeLogicCondition.Or:
							if (prereqChallenge.Value.isComplete)
								shouldBeActivated = true;
							break;
					}
				}
				else break;
			}

			if (challenge.duration == ChallengeDuration.Season)
				playerChallenges.result.activeSeasonChallenge = challengeId;

			playerChallenges.result.challenges[challengeId].state = ChallengeState.Active;

			Log.Information($"[{playerId}]: Activating challenge {challengeId}!");
			WriteChallenges(playerId, playerChallenges);

			return true;
		}

		public static Rewards GetRewardsForChallenge(Guid challengeId) 
			=> StateSingleton.Instance.challengeStorage.challenges[challengeId].challengeInfo.rewards;

		public static ChallengeInfo GetChallengeById(string playerId, Guid challengeId) 
			=> ReadChallenges(playerId).result.challenges[challengeId];

		public static Updates RedeemChallengeForPlayer(string playerId, Guid challengeId)
		{
			var challenge = StateSingleton.Instance.challengeStorage.challenges[challengeId].challengeInfo;
			var playerChallenges = ReadChallenges(playerId);

			playerChallenges.result.challenges[challengeId].isComplete = true;
			playerChallenges.result.challenges[challengeId].state = ChallengeState.Completed;
			playerChallenges.result.challenges[challengeId].percentComplete = 100;

			WriteChallenges(playerId, playerChallenges);

			var completionToken = new Token {clientProperties = new Dictionary<string, string>(), clientType = "challenge.completed", lifetime = "Persistent", rewards = challenge.rewards};
			completionToken.clientProperties.Add("challengeid", challengeId.ToString());
			completionToken.clientProperties.Add("category", challenge.category.GetDisplayName());
			completionToken.clientProperties.Add("expirationtimeutc", playerChallenges.result.challenges[challengeId]?.endTimeUtc.Value.ToString(CultureInfo.InvariantCulture));

			var returnUpdates = new Updates();

			if (TokenUtils.AddToken(playerId, completionToken)) 
				returnUpdates.tokens = GenericUtils.GetNextStreamVersion();

			EventUtils.HandleEvents(playerId, new ChallengeEvent { action = ChallengeEventAction.ChallengeCompleted, eventId = challengeId });

			if (playerChallenges.result.challenges[challengeId].duration == ChallengeDuration.PersonalContinuous)
				RemoveChallengeFromPlayer(playerId, challengeId);

			return returnUpdates;
		}

		public static void GenerateTimedChallenges(string playerId) 
		{
			int maximumTimed = (int)StateSingleton.Instance.settings.result.maximumpersonaltimedchallenges;
			List<Guid> challenges = StateSingleton.Instance.challengeStorage.challenges
				.Where(pred => pred.Value.challengeInfo.duration == ChallengeDuration.PersonalTimed)
				.ToDictionary(pred => pred.Key, pred => pred.Value).Keys.ToList();

			List<Guid> playerChallenges = ReadChallenges(playerId).result.challenges
				.Where(pred => pred.Value.duration == ChallengeDuration.PersonalTimed)
				.ToDictionary(pred => pred.Key, pred => pred.Value).Keys.ToList();

			foreach (var challenge in playerChallenges)
			{
				RemoveChallengeFromPlayer(playerId, challenge);
			}

			if (maximumTimed > challenges.Count)
				maximumTimed = challenges.Count;

			int prevIndex = -1;
			for (int i = 0; i < maximumTimed; i++) 
			{
				int index = random.Next(0, maximumTimed);
				if (prevIndex != index) {
					AddChallengeToPlayer(playerId, challenges[index]);
					prevIndex = index;
				} else {
					i--;
				}
			}
		}

		public static void AddChallengeToPlayer(string playerId, Guid challengeId)
		{
			var challenge = StateSingleton.Instance.challengeStorage.challenges[challengeId].challengeInfo;
			var playerChallenges = ReadChallenges(playerId);

			if (challenge.duration == ChallengeDuration.PersonalTimed)
				challenge.endTimeUtc = DateTime.UtcNow.Date.AddDays(1);

			if (!playerChallenges.result.challenges.ContainsKey(challengeId))
				playerChallenges.result.challenges.Add(challengeId, challenge);

			WriteChallenges(playerId, playerChallenges);
		}

		public static void RemoveChallengeFromPlayer(string playerId, Guid challengeId)
		{
			var playerChallenges = ReadChallenges(playerId);

			if (playerChallenges.result.challenges[challengeId] != null)
				playerChallenges.result.challenges.Remove(challengeId);

			WriteChallenges(playerId, playerChallenges);
		}

		public static void ProgressChallenge(string playerId, BaseEvent ev)
		{
			var playerChallenges = ReadChallenges(playerId);
			var challengeIdList = playerChallenges.result.challenges.Keys.ToList();
			List<ChallengeBackend> challengeRequirements = new();
			foreach (Guid id in challengeIdList)
			{
				challengeRequirements.Add(new ChallengeBackend
				{
					challengeBackendInformation = StateSingleton.Instance.challengeStorage.challenges[id].challengeBackendInformation,
					challengeRequirements = StateSingleton.Instance.challengeStorage.challenges[id].challengeRequirements,
					challengeInfo = playerChallenges.result.challenges[id]
				});
			}

			var challengesToProgress = challengeRequirements.Where(pred =>
				!pred.challengeInfo.isComplete && 
				(pred.challengeBackendInformation.progressWhenLocked ||
				 pred.challengeInfo.state == ChallengeState.Active))
				.ToList();

			switch (ev)
			{
				case TappableEvent evt:
					var tappable = StateSingleton.Instance.activeTappables[evt.eventId];

					challengesToProgress = challengesToProgress
						.Where(pred => pred.challengeRequirements.tappables?
							.Find(pred => 
								pred.targetTappableTypes == null
								|| pred.targetTappableTypes.Contains(tappable.location.icon)) != null)
						.ToList();

					break;

				case ItemEvent evt:
					var catalogItem =
						StateSingleton.Instance.catalog.result.items.Find(match => match.id == evt.eventId);

					challengesToProgress = challengesToProgress.Where(pred =>
						pred.challengeRequirements.items?.Find(match =>
							(match.location == null || match.location.Contains(evt.location))
							&& match.action.Contains(evt.action) 
							&& (match.targetItems == null 
							    || match.targetItems.itemIds.Contains(evt.eventId)
							    || match.targetItems.tags.Contains(catalogItem.item.journalMetadata.groupKey) 
							    || match.targetItems.rarity.Contains(catalogItem.rarity))) != null)
						.ToList();

					break;

				case ChallengeEvent evt:
					ChallengeInfo challenge = GetChallengeById(playerId, evt.eventId);

					challengesToProgress = challengesToProgress.Where(pred =>
						pred.challengeRequirements.challenges?
							.Find(pred => 
								(pred.targetChallengeIds == null || pred.targetChallengeIds.Contains(evt.eventId)) 
								&& (pred.durations == null || pred.durations.Contains(challenge.duration)) 
								&& (pred.rarities == null || pred.rarities.Contains(challenge.rarity))) != null)
						.ToList();

					break;

				/*case MultiplayerEvent evt:
					challengesToProgress = challengesToProgress.Where(pred =>
						pred.challengeRequirements.eventName == evt.GetType().ToString()
						&& pred.challengeRequirements.eventAction == Enum.GetName(evt.action) &&
						(!pred.challengeRequirements.onlyAdventure || evt.isAdventure))
						.ToList();

					challengesToProgress = challengesToProgress.Where(pred => 
						pred.challengeRequirements.targetIdList.Contains(evt.eventId) 
						&& (pred.challengeRequirements.sourceId == null || pred.challengeRequirements.sourceId == evt.sourceId))
						.ToList();

					break;

				case MobEvent evt:
					challengesToProgress = challengesToProgress.Where(pred =>
						pred.challengeRequirements.eventName == evt.GetType().ToString()
						&& pred.challengeRequirements.eventAction == Enum.GetName(evt.action)
						&& (!pred.challengeRequirements.doneByPlayer || evt.killedByPlayer))
						.ToList();

					challengesToProgress = challengesToProgress.Where(pred => 
						pred.challengeRequirements.targetIdList.Contains(evt.eventId) 
						&& (pred.challengeRequirements.sourceId == null || pred.challengeRequirements.sourceId == evt.killerId))
						.ToList();

					break;*/

			}

			var challengesToRedeem = new List<Guid>();

			foreach (ChallengeBackend challenge in challengesToProgress)
			{
				var id = playerChallenges.result.challenges.First(pred => pred.Value == challenge.challengeInfo).Key;

				Log.Debug($"[{playerId}] Progressing challenge {id}.");

				var info = challenge.challengeInfo;
				if (ev.GetType() == typeof(ItemEvent)) info.currentCount += (int) ((ItemEvent)ev).amount;
				info.currentCount++;
				info.percentComplete = (info.currentCount / info.totalThreshold) * 100;

				if (info.currentCount >= info.totalThreshold) challengesToRedeem.Add(id);

				playerChallenges.result.challenges[id] = info;
			}

			WriteChallenges(playerId, playerChallenges);

			foreach (Guid id in challengesToRedeem) 
				RedeemChallengeForPlayer(playerId, id);

		}

        public static ChallengesResponse ReloadChallenges(string playerId)
        {
            var playerChallenges = ReadChallenges(playerId);
			if (playerChallenges.result.challenges.Where(pred => pred.Value.duration == ChallengeDuration.PersonalTimed).Count() == 0) 
				GenerateTimedChallenges(playerId);
            return playerChallenges;
        }

		private static ChallengesResponse ReadChallenges(string playerId)
		{
			return GenericUtils.ParseJsonFile<ChallengesResponse>(playerId, "challenges");
		}

		private static bool WriteChallenges(string playerId, ChallengesResponse challenges)
		{
			return GenericUtils.WriteJsonFile(playerId, challenges, "challenges");
		}
	}
}
