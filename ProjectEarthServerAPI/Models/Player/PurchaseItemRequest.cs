using System;
using System.Collections.Generic;
using ProjectEarthServerAPI.Models.Buildplate;

namespace ProjectEarthServerAPI.Models.Player
{
	public class PurchaseItemRequest
    {
        public int expectedPurchasePrice { get; set; }
        public Guid itemId { get; set; }
    }
}