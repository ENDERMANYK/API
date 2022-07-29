using System;

namespace ProjectEarthServerAPI.Models
{
	public class BaseEvent
	{
		public Guid eventId { get; set; }
	}

	public class ItemEvent : BaseEvent
	{
		public ItemEventAction action { get; set; }
		public uint amount { get; set; }
		public EventLocation location { get; set; }
	}

	public class MultiplayerEvent : BaseEvent
	{
		public MultiplayerEventAction action { get; set; }
		public Guid targetId { get; set; }
		public Guid? sourceId { get; set; } // Used as item-in-hand when action is not block converted
		public bool isAdventure { get; set; }
	}

	public class ChallengeEvent : BaseEvent
	{
		public ChallengeEventAction action { get; set; }
	}

	public class MobEvent : BaseEvent
	{
		public MobEventAction action { get; set; }
		public bool killedByPlayer { get; set; }
		public Guid killerId { get; set; }
		public string damageSource { get; set; }
		public bool isAdventure { get; set; }
	}

	public class TappableEvent : BaseEvent
	{
		
	}

	public class JournalEvent : BaseEvent
	{

	}

	public enum EventLocation
	{
		Tappable,
		Token,
		Challenge,
		LevelUp,
		Adventure,
		Buildplate,
		Crafting,
		Smelting
	}
	public enum ItemEventAction
	{
		ItemCrafted,
		ItemSmelted,
		ItemAwarded,
		ItemDestroyed,
		ItemJournalEntryUnlocked
	}

	public enum MultiplayerEventAction
	{
		BlockPlaced,
		BlockMined,
		BlockConverted
	}

	public enum ChallengeEventAction
	{
		ChallengeUnlocked,
		ChallengeCompleted
	}

	public enum MobEventAction
	{
		MobKilledByPlayer,
		MobKilled,
		MobSpawned,
		PlayerKilled,
		PlayerSpawned
	}
}
