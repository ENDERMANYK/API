using System;
using ProjectEarthServerAPI.Models;
using ProjectEarthServerAPI.Models.Features;
using ProjectEarthServerAPI.Models.Player;

namespace ProjectEarthServerAPI.Util
{
	public class JournalUtils
	{
		public static bool UpdateEntry(string playerId, InventoryResponse.BaseItem item)
		{
			var baseJournal = ReadJournalForPlayer(playerId);
			var createEntry = !baseJournal.result.inventoryJournal.ContainsKey(item.id);

			if (createEntry)
			{
				var entry = new JournalEntry() {firstSeen = item.unlocked.on, lastSeen = item.seen.on};

				entry.amountCollected = item is InventoryResponse.StackableItem stackableItem
					? (uint)stackableItem.owned
					: (uint)((InventoryResponse.NonStackableItem)item).instances.Count;

				baseJournal.result.inventoryJournal.Add(item.id, entry);

				TokenUtils.AddItemToken(playerId, item.id);
			}
			else
			{
				var entry = baseJournal.result.inventoryJournal[item.id];
				var itemAmount = item is InventoryResponse.StackableItem stackableItem
					? (uint)stackableItem.owned
					: (uint)((InventoryResponse.NonStackableItem)item).instances.Count;

				if (entry.amountCollected > itemAmount) entry.amountCollected = itemAmount;

				entry.lastSeen = item.seen.on;

				baseJournal.result.inventoryJournal[item.id] = entry;
			}

			WriteJournalForPlayer(playerId, baseJournal);

			return true;
		}

		public static void AddActivityLogEntry(string playerId, BaseEvent ev)
		{
			var journal = ReadJournalForPlayer(playerId);

			var activityLogEntry = new Activity
			{
				eventTime = DateTime.UtcNow,
				properties = new ActivityProperties
				{
					duration = ChallengeDuration.Career,
					order = (uint)journal.result.activityLog.Count,
					referenceId = Guid.NewGuid()
				},
				rewards = null,
				scenario = Scenario.CraftingJobCompleted
			};

			switch (ev)
			{
				case ItemEvent evt:
					activityLogEntry.rewards = new Rewards {Inventory = new RewardComponent[0]};
					activityLogEntry.rewards.Inventory[0].Amount = (int) evt.amount;
					activityLogEntry.rewards.Inventory[0].Id = evt.eventId;
					switch (evt.action)
					{
						case ItemEventAction.ItemCrafted:
							activityLogEntry.scenario = Scenario.CraftingJobCompleted;
							break;

						case ItemEventAction.ItemSmelted:
							activityLogEntry.scenario = Scenario.SmeltingJobCompleted;
							break;

					}

					break;

				case ChallengeEvent evt:
					activityLogEntry.rewards = ChallengeUtils.GetRewardsForChallenge(evt.eventId);
					activityLogEntry.scenario = Scenario.ChallengeCompleted;
					break;

				case TappableEvent evt:
					activityLogEntry.rewards = StateSingleton.Instance.activeTappables[evt.eventId].rewards;
					activityLogEntry.scenario = Scenario.TappableCollected;
					break;

				case JournalEvent evt:
					activityLogEntry.rewards = new Rewards();
					activityLogEntry.scenario = Scenario.JournalContentCollected;
					break;


			}

			journal.result.activityLog.Add(activityLogEntry);
			WriteJournalForPlayer(playerId, journal);
		}

		public static JournalResponse ReadJournalForPlayer(string playerId)
			=> GenericUtils.ParseJsonFile<JournalResponse>(playerId, "journal");

		public static void WriteJournalForPlayer(string playerId, JournalResponse data)
			=> GenericUtils.WriteJsonFile(playerId, data, "journal");
	}
}
