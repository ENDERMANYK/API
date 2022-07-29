using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using ProjectEarthServerAPI.Models;
using ProjectEarthServerAPI.Models.Features;
using Serilog;

namespace ProjectEarthServerAPI.Util
{
	public class SmeltingUtils
	{
		private static Recipes recipeList = StateSingleton.Instance.recipes;
		private static CatalogResponse catalog = StateSingleton.Instance.catalog;
		private static Dictionary<string, Dictionary<int, SmeltingSlotInfo>> SmeltingJobs = new();

		public static bool StartSmeltingJob(string playerId, int slot, SmeltingRequest request) // TODO: Check if slot not unlocked (not a big priority)
		{
			recipeList ??= Recipes.FromFile("./data/recipes");
			var currentDateTime = DateTime.UtcNow;

			var recipe = recipeList.result.smelting.Find(match => match.id == request.RecipeId);

			if (!SmeltingJobs.ContainsKey(playerId))
			{
				UtilityBlocksResponse playerUtilityBlocks = UtilityBlockUtils.ReadUtilityBlocks(playerId);
				SmeltingJobs.Add(playerId, playerUtilityBlocks.result.smelting);
			}

			if (recipe != null)
			{
				var itemsToReturn = recipe.returnItems.ToList();

				InventoryUtils.RemoveItemFromInv(playerId, recipe.inputItemId, request.Ingredient.quantity * request.Multiplier); //UNCOMMENT/COMMENT THESE LINES TO ENABLE/DISABLE ITEM REMOVALS

				var fuelInfo = new FuelInfo();
				var burning = new BurningItems();

				if (SmeltingJobs[playerId][slot].burning == null || DateTime.Compare(SmeltingJobs[playerId][slot].burning.burnsUntil.Value, DateTime.UtcNow) < 0)
				{
					InventoryUtils.RemoveItemFromInv(playerId, request.FuelIngredient.itemId, request.FuelIngredient.quantity);
				}
				var burnInfo = catalog.result.items.Find(match => match.id == request.FuelIngredient.itemId).burnRate;
				fuelInfo = new FuelInfo {burnRate = new BurnInfo {burnTime = burnInfo.burnTime * request.FuelIngredient.quantity, heatPerSecond = burnInfo.heatPerSecond}, itemId = request.FuelIngredient.itemId, itemInstanceIds = request.FuelIngredient.itemInstanceIds, quantity = request.FuelIngredient.quantity};
				if (SmeltingJobs[playerId][slot].burning != null) {
					burning = SmeltingJobs[playerId][slot].burning;
				} else {
					burning = new BurningItems
					{
						burnStartTime = currentDateTime,
						burnsUntil = currentDateTime.AddSeconds(fuelInfo.burnRate.burnTime),
						fuel = fuelInfo,
						heatDepleted = 0,
						remainingBurnTime = new TimeSpan(0, 0, fuelInfo.burnRate.burnTime)
					};
				}
				var nextStreamId = GenericUtils.GetNextStreamVersion();

				SmeltingSlotInfo job = new SmeltingSlotInfo
				{
					available = 0,
					boostState = null,
					burning = burning,
					fuel = fuelInfo,
					hasSufficientFuel = (recipe.heatRequired <= fuelInfo.burnRate.burnTime * fuelInfo.burnRate.heatPerSecond * fuelInfo.quantity), // Should always be true, requires special handling if false.
					heatAppliedToCurrentItem = 0,
					completed = 0,
					escrow = new InputItem[] {request.Ingredient},
					nextCompletionUtc = null,
					output = recipe.output,
					recipeId = recipe.id,
					sessionId = request.SessionId,
					state = "Active",
					streamVersion = nextStreamId,
					total = request.Multiplier,
					totalCompletionUtc = currentDateTime.AddSeconds((double)recipe.heatRequired * request.Multiplier / fuelInfo.burnRate.heatPerSecond),
					unlockPrice = null
				};

				job.fuel.quantity = 0; // Mojang pls explain this

				if (request.Multiplier != 1)
				{
					job.nextCompletionUtc = currentDateTime.AddSeconds((double)recipe.heatRequired / fuelInfo.burnRate.heatPerSecond);
				}

				SmeltingJobs[playerId][slot] = job;

				UtilityBlockUtils.UpdateUtilityBlocks(playerId, slot, job);

				Log.Debug($"[{playerId}]: Initiated smelting job in slot {slot}.");

				return true;
			}

			return false;
		}

		public static SmeltingSlotResponse GetSmeltingJobInfo(string playerId, int slot)
		{
			try
			{
				if (!SmeltingJobs.ContainsKey(playerId))
				{
					UtilityBlocksResponse playerUtilityBlocks = UtilityBlockUtils.ReadUtilityBlocks(playerId);
					SmeltingJobs.Add(playerId, playerUtilityBlocks.result.smelting);
				}
				var currentTime = DateTime.UtcNow;
				var job = SmeltingJobs[playerId][slot];
				var recipe = recipeList.result.smelting.Find(match => match.id == job.recipeId & !match.deprecated);
				var updates = new Updates();
				var nextStreamId = GenericUtils.GetNextStreamVersion();

				job.streamVersion = nextStreamId;

				if (job.totalCompletionUtc != null && DateTime.Compare(job.totalCompletionUtc.Value, DateTime.UtcNow) < 0 && job.recipeId != null)
				{
					job.available = job.total - job.completed;
					job.completed += job.available;
					job.nextCompletionUtc = null;
					job.state = "Completed";
					job.escrow = new InputItem[0];
				}
				/*else
				{

				    job.available++;
				    //job.completed++;
				    job.state = "Available";
				    job.streamVersion = nextStreamId;
				    job.nextCompletionUtc = job.nextCompletionUtc.Value.Add(recipe.duration.TimeOfDay);

				    for (int i = 0; i < job.escrow.Length - 1; i++)
				    {
				        job.escrow[i].quantity -= recipe.ingredients[i].quantity;
				    }

				}*/

				if (recipe != null)
				{
					job.burning.remainingBurnTime = job.burning.burnsUntil.Value.TimeOfDay - currentTime.TimeOfDay;

					job.burning.heatDepleted = (currentTime - job.burning.burnStartTime.Value).TotalSeconds *
											   job.burning.fuel.burnRate.heatPerSecond;

					job.heatAppliedToCurrentItem =
						(float)job.burning.heatDepleted - job.available * recipe.heatRequired;
				}

				updates.smelting = nextStreamId;


				var returnResponse = new SmeltingSlotResponse {result = job, updates = updates};

				UtilityBlockUtils.UpdateUtilityBlocks(playerId, slot, job);

				Log.Debug($"[{playerId}]: Requested smelting slot {slot} status.");

				return returnResponse;
			}
			catch (Exception e)
			{
				Log.Error($"[{playerId}]: Error while getting smelting job info: Smelting Slot: {slot}");
				Log.Debug($"Exception: {e.StackTrace}");
				return null;
			}
		}

		public static CollectItemsResponse FinishSmeltingJob(string playerId, int slot)
		{
			if (!SmeltingJobs.ContainsKey(playerId))
			{
				UtilityBlocksResponse playerUtilityBlocks = UtilityBlockUtils.ReadUtilityBlocks(playerId);
				SmeltingJobs.Add(playerId, playerUtilityBlocks.result.smelting);
			}
			var job = SmeltingJobs[playerId][slot];
			var recipe = recipeList.result.smelting.Find(match => match.id == job.recipeId & !match.deprecated);
			var currentTime = DateTime.UtcNow;
			int craftedAmount = 0;

			var nextStreamId = GenericUtils.GetNextStreamVersion();

			var returnResponse = new CollectItemsResponse {result = new CollectItemsInfo {rewards = new Rewards(),}, updates = new Updates()};

			if (job.completed != job.total && job.nextCompletionUtc != null)
			{
				if (DateTime.UtcNow >= job.nextCompletionUtc)
				{
					craftedAmount++;
					while (DateTime.UtcNow >= job.nextCompletionUtc && job.nextCompletionUtc.Value.AddSeconds((double)recipe.heatRequired / job.burning.fuel.burnRate.heatPerSecond) < job.totalCompletionUtc && craftedAmount < job.total - job.completed)
					{
						job.nextCompletionUtc = job.nextCompletionUtc.Value.AddSeconds((double)recipe.heatRequired / job.burning.fuel.burnRate.heatPerSecond);
						craftedAmount++;
					}

					job.nextCompletionUtc = job.nextCompletionUtc.Value.AddSeconds((double)recipe.heatRequired / job.burning.fuel.burnRate.heatPerSecond);
					job.completed += craftedAmount;
					//job.available -= craftedAmount;
					foreach (var inputItem in job.escrow)
					{
						inputItem.quantity -= 1;
					}

					job.streamVersion = nextStreamId;

				}
			}
			else
			{
				craftedAmount = job.available;
				// TODO: Add to challenges, tokens, journal (when implemented)
			}

			InventoryUtils.AddItemToInv(playerId, job.output.itemId, job.output.quantity * craftedAmount);
			EventUtils.HandleEvents(playerId, new ItemEvent { action = ItemEventAction.ItemSmelted, amount = (uint)(job.output.quantity * craftedAmount), eventId = job.output.itemId, location = EventLocation.Smelting});

			returnResponse.result.rewards.Inventory = returnResponse.result.rewards.Inventory.Append(new RewardComponent {Amount = job.output.quantity * craftedAmount, Id = job.output.itemId}).ToArray();

			returnResponse.updates.smelting = nextStreamId;
			returnResponse.updates.inventory = nextStreamId;
			returnResponse.updates.playerJournal = nextStreamId;


			if (job.completed == job.total || job.nextCompletionUtc == null)
			{
				job.burning.remainingBurnTime = new TimeSpan((job.burning.burnsUntil - currentTime.TimeOfDay).Value.Ticks);
				job.burning.heatDepleted = (currentTime - job.burning.burnStartTime.Value).TotalSeconds *
										   job.burning.fuel.burnRate.heatPerSecond;

				job.fuel = null;
				job.heatAppliedToCurrentItem = null;
				job.hasSufficientFuel = null;
				job.nextCompletionUtc = null;
				job.available = 0;
				job.completed = 0;
				job.recipeId = null;
				job.sessionId = null;
				job.state = "Empty";
				job.total = 0;
				job.boostState = null;
				job.totalCompletionUtc = null;
				job.unlockPrice = null;
				job.output = null;
				job.streamVersion = nextStreamId;
			}

			UtilityBlockUtils.UpdateUtilityBlocks(playerId, slot, job);

			Log.Debug($"[{playerId}]: Collected results of smelting slot {slot}.");

			return returnResponse;
		}

		public static bool CancelSmeltingJob(string playerId, int slot)
		{
			if (!SmeltingJobs.ContainsKey(playerId))
			{
				UtilityBlocksResponse playerUtilityBlocks = UtilityBlockUtils.ReadUtilityBlocks(playerId);
				SmeltingJobs.Add(playerId, playerUtilityBlocks.result.smelting);
			}
			var job = SmeltingJobs[playerId][slot];
			var currentTime = DateTime.UtcNow;
			var nextStreamId = GenericUtils.GetNextStreamVersion();

			foreach (InputItem item in job.escrow)
			{
				InventoryUtils.AddItemToInv(playerId, item.itemId, item.quantity);
			}

			job.burning.remainingBurnTime = new TimeSpan((job.burning.burnsUntil - currentTime.TimeOfDay).Value.Ticks);
			job.burning.heatDepleted = (currentTime - job.burning.burnStartTime.Value).TotalSeconds *
									   job.burning.fuel.burnRate.heatPerSecond;

			job.burning.burnStartTime = null;
			job.burning.burnsUntil = null;

			job.fuel = null;
			job.heatAppliedToCurrentItem = null;
			job.hasSufficientFuel = null;
			job.nextCompletionUtc = null;
			job.available = 0;
			job.completed = 0;
			job.recipeId = null;
			job.sessionId = null;
			job.state = "Empty";
			job.total = 0;
			job.boostState = null;
			job.totalCompletionUtc = null;
			job.unlockPrice = null;
			job.output = null;
			job.streamVersion = nextStreamId;

			UtilityBlockUtils.UpdateUtilityBlocks(playerId, slot, job);

			Log.Debug($"[{playerId}]: Cancelled smelting job in slot {slot}.");

			return true;
		}

		public static CraftingUpdates UnlockSmeltingSlot(string playerId, int slot)
		{
			if (!SmeltingJobs.ContainsKey(playerId))
			{
				UtilityBlocksResponse playerUtilityBlocks = UtilityBlockUtils.ReadUtilityBlocks(playerId);
				SmeltingJobs.Add(playerId, playerUtilityBlocks.result.smelting);
			}
			var job = SmeltingJobs[playerId][slot];

			RubyUtils.RemoveRubiesFromPlayer(playerId, job.unlockPrice.cost - job.unlockPrice.discount);
			job.state = "Empty";
			job.unlockPrice = null;

			var nextStreamId = GenericUtils.GetNextStreamVersion();
			var returnUpdates = new CraftingUpdates {updates = new Updates()};

			UtilityBlockUtils.UpdateUtilityBlocks(playerId, slot, job);

			Log.Debug($"[{playerId}]: Unlocked smelting slot {slot}.");

			returnUpdates.updates.smelting = nextStreamId;

			return returnUpdates;
		}
	}
}
