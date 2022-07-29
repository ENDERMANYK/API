using System;
using System.IO;
using Newtonsoft.Json;
using ProjectEarthServerAPI.Models;
using ProjectEarthServerAPI.Models.Player;

namespace ProjectEarthServerAPI.Util
{
	public class RubyUtils
	{
		public static SplitRubyResponse ReadRubies(string playerId)
		{
			return GenericUtils.ParseJsonFile<SplitRubyResponse>(playerId, "rubies");
		}

		public static bool WriteRubies(string playerId, SplitRubyResponse ruby)
		{
			return GenericUtils.WriteJsonFile(playerId, ruby, "rubies");
		}

		public static bool AddRubiesToPlayer(string playerId, int count)
		{
			var origRubies = ReadRubies(playerId);
			origRubies.result.earned += count;

			var newRubyNum = origRubies.result.earned;

			WriteRubies(playerId, origRubies);

			return true;
		}

		public static bool RemoveRubiesFromPlayer(string playerId, int count)
		{
			var origRubies = ReadRubies(playerId);
			origRubies.result.earned -= count;

			var newRubyNum = origRubies.result.earned;

			WriteRubies(playerId, origRubies);

			return true;
		}

		public static RubyResponse GetNormalRubyResponse(string playerid)
		{
			var splitrubies = ReadRubies(playerid);
			var response = new RubyResponse() {
				result = splitrubies.result.earned + splitrubies.result.purchased,
				expiration = null,
				continuationToken = null,
				updates = new Updates()
			};

			return response;
		}
	}
}
