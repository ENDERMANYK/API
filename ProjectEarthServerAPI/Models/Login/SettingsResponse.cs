using System.IO;
using Newtonsoft.Json;

namespace ProjectEarthServerAPI.Models
{
	public class SettingsResponse
	{
		public SettingsResult result { get; set; }
		public object expiration { get; set; }
		public object continuationToken { get; set; }
		public Updates updates { get; set; }

		public static SettingsResponse FromFile(string path)
		{
			var jsontext = File.ReadAllText(path);
			return JsonConvert.DeserializeObject<SettingsResponse>(jsontext);
		}
	}

	public class SettingsResult
	{
		public double encounterinteractionradius { get; set; }
		public double tappableinteractionradius { get; set; }
		public double tappablevisibleradius { get; set; }
		public double targetpossibletappables { get; set; }
		public double tile0 { get; set; }
		public double slowrequesttimeout { get; set; }
		public double cullingradius { get; set; }
		public double commontapcount { get; set; }
		public double epictapcount { get; set; }
		public double speedwarningcooldown { get; set; }
		public double mintappablesrequiredpertile { get; set; }
		public double targetactivetappables { get; set; }
		public double tappablecullingradius { get; set; }
		public double raretapcount { get; set; }
		public double requestwarningtimeout { get; set; }
		public double speedwarningthreshold { get; set; }
		public double asaanchormaxplaneheightthreshold { get; set; }
		public double maxannouncementscount { get; set; }
		public double removethislater { get; set; }
		public double crystalslotcap { get; set; }
		public double crystaluncommonduration { get; set; }
		public double crystalrareduration { get; set; }
		public double crystalepicduration { get; set; }
		public double crystalcommonduration { get; set; }
		public double crystallegendaryduration { get; set; }
		public double maximumpersonaltimedchallenges { get; set; }
		public double maximumpersonalcontinuouschallenges { get; set; }
	}
}
