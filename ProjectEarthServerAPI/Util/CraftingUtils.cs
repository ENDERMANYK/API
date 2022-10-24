using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using ProjectEarthServerAPI.Models;
using ProjectEarthServerAPI.Models.Features;
using Serilog;

namespace ProjectEarthServerAPI.Util
{
	public class CraftingUtils // TODO: Bug Fix: Cancelling a crafting task that you already collected some results
							   // TODO: for returns the entire crafting cost, not just the remaining part
	{
		private static Recipes recipeList = StateSingleton.Instance.recipes;
		private static Dictionary<string, Dictionary<int, CraftingSlotInfo>> craftingJobs = new();
		public static bool StartCraftingJob(string playerId, int slot, CraftingRequest request) // TODO: Check if slot not unlocked (not a big priority)
		{
			recipeList ??= Recipes.FromFile("./data/recipes");

			var recipe = recipeList.result.crafting.Find(match => match.id == request.RecipeId);

			if (recipe != null)
			{
				var itemsToReturn = recipe.returnItems.ToList();

				foreach (RecipeIngredients ingredient in recipe.ingredients)
				{
					if (itemsToReturn.Find(match =>
						match.id == ingredient.items[0] && match.amount == ingredient.quantity) == null)
					{
						InventoryUtils.RemoveItemFromInv(playerId, ingredient.items[0], ingredient.quantity * request.Multiplier);
					}
				}

				var nextStreamId = GenericUtils.GetNextStreamVersion();

				CraftingSlotInfo job = new CraftingSlotInfo
				{
					available = 0,
					boostState = null,
					completed = 0,
					escrow = request.Ingredients,
					nextCompletionUtc = null,
					output = recipe.output,
					recipeId = recipe.id,
					sessionId = request.SessionId,
					state = "Active",
					streamVersion = nextStreamId,
					total = request.Multiplier,
					totalCompletionUtc = DateTime.UtcNow.Add(recipe.duration.TimeOfDay * request.Multiplier),
					unlockPrice = null

				};

				if (request.Multiplier != 1)
				{
					job.nextCompletionUtc = DateTime.UtcNow.Add(recipe.duration.TimeOfDay);
				}

				if (!craftingJobs.ContainsKey(playerId))
				{
					UtilityBlocksResponse playerUtilityBlocks = UtilityBlockUtils.ReadUtilityBlocks(playerId);
					craftingJobs.Add(playerId, playerUtilityBlocks.result.crafting);
				}

				craftingJobs[playerId][slot] = job;

				UtilityBlockUtils.UpdateUtilityBlocks(playerId, slot, job);

				Log.Debug($"[{playerId}]: Initiated crafting job in slot {slot}.");

				return true;
			}

			return false;

		}

		public static CraftingSlotResponse GetCraftingJobInfo(string playerId, int slot)
		{

			try
			{
				if (!craftingJobs.ContainsKey(playerId))
				{
					UtilityBlocksResponse playerUtilityBlocks = UtilityBlockUtils.ReadUtilityBlocks(playerId);
					craftingJobs.Add(playerId, playerUtilityBlocks.result.crafting);
				}
				var job = craftingJobs[playerId][slot];
				var recipe = recipeList.result.crafting.Find(match => match.id == job.recipeId & !match.deprecated);
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

				updates.crafting = nextStreamId;

				var returnResponse = new CraftingSlotResponse
				{
					result = job,
					updates = updates
				};

				UtilityBlockUtils.UpdateUtilityBlocks(playerId, slot, job);

				Log.Debug($"[{playerId}]: Requested crafting slot {slot} status.");

				return returnResponse;
			}
			catch (Exception e)
			{
				Log.Error($"[{playerId}]: Error while getting crafting job info! Crafting Slot: {slot}");
				Log.Debug($"Exception: {e.StackTrace}");
				return null;
			}

		}

		public static SplitRubyResponse FinishCraftingJobNow(string playerId, int slot)
		{
			var job = craftingJobs[playerId][slot];

			job.streamVersion = GenericUtils.GetNextStreamVersion();
			job.available = job.total - job.completed;
			job.completed += job.available;
			job.nextCompletionUtc = null;
			job.state = "Completed";
			job.escrow = new InputItem[0];

			UtilityBlockUtils.UpdateUtilityBlocks(playerId, slot, job);

			return RubyUtils.ReadRubies(playerId);
		}

		public static CollectItemsResponse FinishCraftingJob(string playerId, int slot)
		{
			if (!craftingJobs.ContainsKey(playerId))
			{
				UtilityBlocksResponse playerUtilityBlocks = UtilityBlockUtils.ReadUtilityBlocks(playerId);
				craftingJobs.Add(playerId, playerUtilityBlocks.result.crafting);
			}
			var job = craftingJobs[playerId][slot];
			var recipe = recipeList.result.crafting.Find(match => match.id == job.recipeId & !match.deprecated);
			int craftedAmount = 0;

			var nextStreamId = GenericUtils.GetNextStreamVersion();

			var returnResponse = new CollectItemsResponse
			{
				result = new CollectItemsInfo
				{
					rewards = new Rewards(),
				},
				updates = new Updates()
			};

			if (job.completed != job.total && job.nextCompletionUtc != null)
			{
				if (DateTime.UtcNow >= job.nextCompletionUtc)
				{
					craftedAmount++;
					while (DateTime.UtcNow >= job.nextCompletionUtc && job.nextCompletionUtc.Value.Add(recipe.duration.TimeOfDay) < job.totalCompletionUtc && craftedAmount < job.total - job.completed)
					{
						job.nextCompletionUtc = job.nextCompletionUtc.Value.Add(recipe.duration.TimeOfDay);
						craftedAmount++;
					}

					job.nextCompletionUtc = job.nextCompletionUtc.Value.Add(recipe.duration.TimeOfDay);
					job.completed += craftedAmount;
					//job.available -= craftedAmount;
					for (int i = 0; i < job.escrow.Length; i++)
					{
						job.escrow[i].quantity -= recipe.ingredients[i].quantity * craftedAmount;
					}

					job.streamVersion = nextStreamId;
				}
			}
			else
			{
				craftedAmount = job.available;
			}

			InventoryUtils.AddItemToInv(playerId, job.output.itemId, job.output.quantity * craftedAmount);
			EventUtils.HandleEvents(playerId, new ItemEvent { action = ItemEventAction.ItemCrafted, amount = (uint)(job.output.quantity * craftedAmount), eventId = job.output.itemId, location = EventLocation.Crafting });

			returnResponse.result.rewards.Inventory = returnResponse.result.rewards.Inventory.Append(new RewardComponent
			{
				Amount = job.output.quantity * craftedAmount,
				Id = job.output.itemId
			}).ToArray();

			returnResponse.updates.crafting = nextStreamId;
			returnResponse.updates.inventory = nextStreamId;
			returnResponse.updates.playerJournal = nextStreamId;


			if (job.completed == job.total || job.nextCompletionUtc == null)
			{
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

			Log.Debug($"[{playerId}]: Collected results of crafting slot {slot}.");

			return returnResponse;

		}

		public static CraftingSlotResponse CancelCraftingJob(string playerId, int slot)
		{
			if (!craftingJobs.ContainsKey(playerId))
			{
				UtilityBlocksResponse playerUtilityBlocks = UtilityBlockUtils.ReadUtilityBlocks(playerId);
				craftingJobs.Add(playerId, playerUtilityBlocks.result.crafting);
			}
			var job = craftingJobs[playerId][slot];
			var nextStreamId = GenericUtils.GetNextStreamVersion();
			var resp = new CraftingSlotResponse
			{
				result = new CraftingSlotInfo(),
				updates = new Updates()
			};

			foreach (InputItem item in job.escrow)
			{
				InventoryUtils.AddItemToInv(playerId, item.itemId, item.quantity);
			}

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

			Log.Debug($"[{playerId}]: Cancelled crafting job in slot {slot}.");

			resp.updates.crafting = nextStreamId;
			return resp;
		}

		public static CraftingUpdates UnlockCraftingSlot(string playerId, int slot)
		{
			if (!craftingJobs.ContainsKey(playerId))
			{
				UtilityBlocksResponse playerUtilityBlocks = UtilityBlockUtils.ReadUtilityBlocks(playerId);
				craftingJobs.Add(playerId, playerUtilityBlocks.result.crafting);
			}
			var job = craftingJobs[playerId][slot];

			RubyUtils.RemoveRubiesFromPlayer(playerId, job.unlockPrice.cost - job.unlockPrice.discount);

			job.state = "Empty";
			job.unlockPrice = null;

			var nextStreamId = GenericUtils.GetNextStreamVersion();
			var returnUpdates = new CraftingUpdates
			{
				updates = new Updates()
			};

			UtilityBlockUtils.UpdateUtilityBlocks(playerId, slot, job);

			Log.Debug($"[{playerId}]: Unlocked crafting slot {slot}.");

			returnUpdates.updates.crafting = nextStreamId;

			return returnUpdates;

		}
	}
}
