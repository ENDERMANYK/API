namespace ProjectEarthServerAPI.Models.Buildplate
{
	public class BuildplateServerRequest
	{
		public double coordinateAccuracyVariance { get; set; }
		public Coordinate playerCoordinate { get; set; }
	}
	public class SharedBuildplateServerRequest : BuildplateServerRequest
	{
		public bool fullSize { get; set; }
	}
	public class MultiplayerJoinRequest : BuildplateServerRequest 
	{
		public string id { get; set; }
		public int? minHotbarStreamVersion { get; set; }
	}

	public class EncounterServerRequest : BuildplateServerRequest
	{
		public string tileId { get; set; }
	}
}
