using System;
using System.Collections.Generic;
using System.Data.Common;
using ProjectEarthServerAPI.Util;

namespace ProjectEarthServerAPI.Models.Features
{
	public class UtilityBlocksResponse
	{
		public Result result { get; set; }
		public Updates updates { get; set; }

		public class Result
		{
			public Dictionary<int, CraftingSlotInfo> crafting { get; set; }
			public Dictionary<int, SmeltingSlotInfo> smelting { get; set; }
		}

		public UtilityBlocksResponse()
		{
			result = new Result {crafting = new Dictionary<int, CraftingSlotInfo>(), smelting = new Dictionary<int, SmeltingSlotInfo>()};

			var nextStreamId = GenericUtils.GetNextStreamVersion();

			result.crafting.Add(1, new CraftingSlotInfo());
			result.crafting.Add(2, new CraftingSlotInfo());
			result.crafting.Add(3, new CraftingSlotInfo());

			result.smelting.Add(1, new SmeltingSlotInfo());
			result.smelting.Add(2, new SmeltingSlotInfo());
			result.smelting.Add(3, new SmeltingSlotInfo());

			result.crafting[1].state = "Empty";

			result.crafting[2].state = "Locked";
			result.crafting[2].unlockPrice = new UnlockPrice {cost = 10, discount = 0};
			result.crafting[3].state = "Locked";
			result.crafting[3].unlockPrice = new UnlockPrice {cost = 15, discount = 1};

			result.crafting[1].escrow = new InputItem[0];
			result.crafting[2].escrow = new InputItem[0];
			result.crafting[3].escrow = new InputItem[0];

			result.crafting[1].streamVersion = nextStreamId;
			result.crafting[2].streamVersion = nextStreamId;
			result.crafting[3].streamVersion = nextStreamId;


			result.smelting[1].state = "Empty";

			result.smelting[2].state = "Locked";
			result.smelting[2].unlockPrice = new UnlockPrice {cost = 10, discount = 0};
			result.smelting[3].state = "Locked";
			result.smelting[3].unlockPrice = new UnlockPrice {cost = 15, discount = 1};

			result.smelting[1].escrow = new InputItem[0];
			result.smelting[2].escrow = new InputItem[0];
			result.smelting[3].escrow = new InputItem[0];

			result.smelting[1].streamVersion = nextStreamId;
			result.smelting[2].streamVersion = nextStreamId;
			result.smelting[3].streamVersion = nextStreamId;

			updates = new Updates();
		}
	}
}
