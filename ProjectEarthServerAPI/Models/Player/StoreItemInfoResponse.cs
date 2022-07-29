using System;
using System.Collections.Generic;
using ProjectEarthServerAPI.Models.Buildplate;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace ProjectEarthServerAPI.Models.Player
{
	public class StoreItemInfoResponse
	{
		public List<StoreItemInfo> result { get; set; }
		public object expiration { get; set; }
		public object continuationToken { get; set; }
		public Updates updates { get; set; }  
	}

	public class StoreItemInfo
	{
		public Guid id { get; set; }

		[JsonConverter(typeof(StringEnumConverter))]
		public StoreItemType storeItemType { get; set; }

		[JsonConverter(typeof(StringEnumConverter))]
		public StoreItemStatus? status { get; set; }
		public uint streamVersion { get; set; }
		public string model { get; set; }
		public Offset buildplateWorldOffset { get; set; }
		public Dimension buildplateWorldDimension { get; set; }
		public Dictionary<Guid, int> inventoryCounts { get; set; } 
		public Guid? featuredItem { get; set; }
	}
	public enum StoreItemType
	{
		Buildplates,
		Items
	}
	public enum StoreItemStatus
	{
		Found,
		NotFound,
		NotModified
	}
}