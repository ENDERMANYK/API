using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using ProjectEarthServerAPI.Models.Player;
using ProjectEarthServerAPI.Util;

namespace ProjectEarthServerAPI.Models.Buildplate
{
    public class ShareBuildplateResponse 
    {
        public string result { get; set; }
		public object expiration { get; set; }
		public object continuationToken { get; set; }
		public Updates updates { get; set; }     
    }
    public class SharedBuildplateResponse 
    {
        public SharedBuildplateInfo result { get; set; }
		public object expiration { get; set; }
		public object continuationToken { get; set; }
		public Updates updates { get; set; }     
    }
    public class SharedBuildplateInfo 
    {
        public string playerId { get; set; }
        public SharedBuildplateData buildplateData { get; set; }
        public InventoryResponse.Result inventory { get; set; }

        [JsonConverter(typeof(DateTimeConverter))]
        public DateTime sharedOn { get; set; }
    }
    public class SharedBuildplateData
    {
        public string type { get; set; }
        public string model { get; set; }
        public Offset offset { get; set; }
        public Dimension dimension { get; set; }
        public double blocksPerMeter { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
		public SurfaceOrientation surfaceOrientation { get; set; }
        public int order { get; set; }
    }
}