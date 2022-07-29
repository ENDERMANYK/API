namespace ProjectEarthServerAPI.Models
{
	public class RubyResponse
	{
		public int result { get; set; }
		public object expiration { get; set; }
		public object continuationToken { get; set; }
		public Updates updates { get; set; }
	}

	public class SplitRubyResponse
	{
		public SplitRubyResult result { get; set; }
		public object expiration { get; set; }
		public object continuationToken { get; set; }
		public Updates updates { get; set; }

		public SplitRubyResponse()
		{
			result = new SplitRubyResult {earned = 0, purchased = 0};
			expiration = null;
			continuationToken = null;
			updates = new Updates();
		}
	}

	public class SplitRubyResult
	{
		public int purchased { get; set; }
		public int earned { get; set; }
	}
}
