using System;
using System.Collections.Generic;
using static ProjectEarthServerAPI.Models.LocationResponse;

namespace ProjectEarthServerAPI.Models
{
	public class ScrollsResponse
	{
		public List<ActiveLocation> result { get; set; }
		public object expiration { get; set; }
		public object continuationToken { get; set; }
		public Updates updates { get; set; }
	}
}
