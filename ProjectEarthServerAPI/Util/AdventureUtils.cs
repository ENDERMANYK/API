using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using ProjectEarthServerAPI.Models;
using ProjectEarthServerAPI.Models.Features;
using ProjectEarthServerAPI.Models.Multiplayer;
using ProjectEarthServerAPI.Models.Multiplayer.Adventure;
using Serilog;

namespace ProjectEarthServerAPI.Util
{
	public class AdventureUtils
	{

		public static List<LocationResponse.ActiveLocation> Encounters = new List<LocationResponse.ActiveLocation>();

		public static string[] AdventureIcons = new[] 
		{
			"genoa:adventure_generic_map", "genoa:adventure_generic_map_b", "genoa:adventure_generic_map_c"
		};

		private static Random random = new Random();

		public Dictionary<Guid, Item.Rarity> crystalRarityList = StateSingleton.Instance.catalog.result.items
			.FindAll(select => select.item.type == "AdventureScroll")
			.ToDictionary(pred => pred.id, pred => pred.rarity);

		public static AdventureRequestResult RedeemCrystal(string playerId, PlayerAdventureRequest adventureRequest, Guid crystalId)
		{
			InventoryUtils.RemoveItemFromInv(playerId, crystalId);

			var selectedAdventureIcon = AdventureIcons[random.Next(0, AdventureIcons.Length)];
			var selectedAdventureId = Guid.Parse("b7335819-c123-49b9-83fb-8a0ec5032779");

			var adventureLocation = new LocationResponse.ActiveLocation
			{
				coordinate = adventureRequest.coordinate,
				encounterMetadata = new EncounterMetadata
				{
					anchorId = "",
					anchorState = "Off",
					augmentedImageSetId = "",
					encounterType = EncounterType.None,
					locationId = Guid.Empty,
					worldId = Guid.Parse("4f16a053-4929-263a-c91a-29663e29df76") // TODO: Replace this with actual adventure id
				},
				expirationTime = DateTime.UtcNow.Add(TimeSpan.FromMinutes(10.00)),
				spawnTime = DateTime.UtcNow,
				icon = selectedAdventureIcon,
				id = selectedAdventureId,
				metadata = new LocationResponse.Metadata
				{
					rarity = Item.Rarity.Common,
					rewardId = "genoa:adventure_rewards" 
				},
				tileId = Tile.getTileForCoordinates(adventureRequest.coordinate.latitude, adventureRequest.coordinate.longitude),
				type = "PlayerAdventure"
			};

			return new AdventureRequestResult {result = adventureLocation, updates = new Updates()};
		}

		public static List<Coordinate> readEncounterLocations() {
            var filepath = StateSingleton.Instance.config.EncounterLocationsFileLocation;
            var encouterLocationsJson = File.ReadAllText(filepath);
            return JsonConvert.DeserializeObject<List<Coordinate>>(encouterLocationsJson);
        }

		public static List<LocationResponse.ActiveLocation> GetEncountersForLocation(double lat, double lon) {
			var encouterLocations = readEncounterLocations();

			Encounters.RemoveAll(match => match.expirationTime < DateTime.UtcNow );

			foreach (var coordinate in encouterLocations)
			{
				if (Encounters.FirstOrDefault(match => match.coordinate.latitude == coordinate.latitude && match.coordinate.longitude == coordinate.longitude) == null) {
					var selectedAdventureIcon = AdventureIcons[random.Next(0, AdventureIcons.Length)];
					var selectedAdventureId = Guid.Parse("b7335819-c123-49b9-83fb-8a0ec5032779");
					var currentTime = DateTime.UtcNow;
					Encounters.Add(new LocationResponse.ActiveLocation
					{
						coordinate = coordinate,
						encounterMetadata = new Models.Multiplayer.Adventure.EncounterMetadata
						{
							anchorId = "",
							anchorState = "Off",
							augmentedImageSetId = "",
							encounterType = EncounterType.Short16X16Hostile,
							locationId = selectedAdventureId,
							worldId = selectedAdventureId // TODO: Replace this with actual adventure id
						},
						expirationTime = currentTime.Add(TimeSpan.FromMinutes(10.00)),
						spawnTime = currentTime,
						icon = selectedAdventureIcon,
						id = selectedAdventureId,
						metadata = new LocationResponse.Metadata
						{
							rarity = Item.Rarity.Common,
							rewardId = "genoa:adventure_rewards"//version4Generator.NewUuid().ToString() // Seems to always be uuidv4 from official responses so generate one
						},
						tileId = Tile.getTileForCoordinates(coordinate.latitude, coordinate.longitude),
						type = "Encounter"
					});
				}
			}
			return Encounters;
		}
	}
}
