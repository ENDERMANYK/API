namespace ProjectEarthServerAPI.Models
{
	public class FeaturesResponse
	{
		public FeaturesResult result { get; set; }
		public string expiration { get; set; }
		public string continuationTokem { get; set; }
		public Updates updates { get; set; }
	}

	public class FeaturesResult
	{
		public bool workshop_enabled { get; set; } = true;
		public bool buildplates_enabled { get; set; } = true;
		public bool enable_ruby_purchasing { get; set; } = false;
		public bool commerce_enabled { get; set; } = true;
		public bool full_logging_enabled { get; set; } = true;
		public bool challenges_enabled { get; set; } = true;
		public bool craftingv2_enabled { get; set; } = true;
		public bool smeltingv2_enabled { get; set; } = true;
		public bool inventory_item_boosts_enabled { get; set; } = true;
		public bool player_health_enabled { get; set; } = true;
		public bool minifigs_enabled { get; set; } = true;
		public bool potions_enabled { get; set; } = true;
		public bool social_link_share_enabled { get; set; } = true;
		public bool social_link_launch_enabled { get; set; } = true;
		public bool encoded_join_enabled { get; set; } = true;
		public bool adventure_crystals_enabled { get; set; } = true;
		public bool item_limits_enabled { get; set; } = true;
		public bool adventure_crystals_ftue_enabled { get; set; } = true;
		public bool expire_crystals_on_cleanup_enabled { get; set; } = true;
		public bool challenges_v2_enabled { get; set; } = true;
		public bool player_journal_enabled { get; set; } = true;
		public bool player_stats_enabled { get; set; } = true;
		public bool activity_log_enabled { get; set; } = true;
		public bool seasons_enabled { get; set; } = true;
		public bool daily_login_enabled { get; set; } = true;
		public bool store_pdp_enabled { get; set; } = true;
		public bool hotbar_stacksplitting_enabled { get; set; } = true;
		public bool fancy_rewards_screen_enabled { get; set; } = true;
		public bool async_ecs_dispatcher { get; set; } = true;
		public bool adventure_oobe_enabled { get; set; } = true;
		public bool tappable_oobe_enabled { get; set; } = true;
		public bool map_permission_oobe_enabled { get; set; } = true;
		public bool journal_oobe_enabled { get; set; } = true;
		public bool freedom_oobe_enabled { get; set; } = true;
		public bool challenge_oobe_enabled { get; set; } = true;
		public bool level_rewards_v2_enabled { get; set; } = true;
		public bool content_driven_season_assets { get; set; } = true;
		public bool paid_earned_rubies_enabled { get; set; } = true;
	}
}
