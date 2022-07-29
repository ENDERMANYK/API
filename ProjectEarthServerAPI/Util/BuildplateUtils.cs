using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using ProjectEarthServerAPI.Models.Buildplate;
using ProjectEarthServerAPI.Models.Multiplayer;
using ProjectEarthServerAPI.Models.Player;
using Serilog;
using Uma.Uuid;

namespace ProjectEarthServerAPI.Util
{
	public class BuildplateUtils
	{
		private static Version4Generator version4Generator = new Version4Generator();

		public static BuildplateListResponse GetBuildplatesList(string playerId)
		{
			var buildplates = ReadPlayerBuildplateList(playerId);
			BuildplateListResponse list = new BuildplateListResponse {result = new List<BuildplateData>()};
			foreach (Guid id in buildplates.UnlockedBuildplates)
			{
				var bp = ReadBuildplate(id);
				bp.order = buildplates.UnlockedBuildplates.IndexOf(id);
				list.result.Add(bp.id != bp.templateId ? ReadBuildplate(id) : CloneTemplateBuildplate(playerId, bp));
			}

			buildplates.LockedBuildplates.ForEach(action =>
			{
				var bp = ReadBuildplate(action);
				bp.order = buildplates.LockedBuildplates.IndexOf(action) + buildplates.UnlockedBuildplates.Count;
				list.result.Add(bp);
			});

			return list;
		}

		public static PlayerBuildplateList ReadPlayerBuildplateList(string playerId)
			=> GenericUtils.ParseJsonFile<PlayerBuildplateList>(playerId, "buildplates");

		public static void WritePlayerBuildplateList(string playerId, PlayerBuildplateList list)
			=> GenericUtils.WriteJsonFile(playerId, list, "buildplates");

		public static void AddToPlayer(string playerId, Guid buildplateId)
		{
			var bpList = ReadPlayerBuildplateList(playerId);

			if (!bpList.UnlockedBuildplates.Contains(buildplateId))
				bpList.UnlockedBuildplates.Add(buildplateId);

			WritePlayerBuildplateList(playerId, bpList);
		}

		public static BuildplateData CloneTemplateBuildplate(string playerId, BuildplateData templateBuildplate)
		{
			var clonedId = Guid.NewGuid();
			BuildplateData clonedBuildplate = templateBuildplate;
			clonedBuildplate.id = clonedId;
			clonedBuildplate.locked = false;

			WriteBuildplate(clonedBuildplate);

			var list = ReadPlayerBuildplateList(playerId);
			var index = list.UnlockedBuildplates.IndexOf(templateBuildplate.id);
			list.UnlockedBuildplates.Remove(templateBuildplate.id);
			list.UnlockedBuildplates.Insert(index, clonedId);

			WritePlayerBuildplateList(playerId, list);

			return clonedBuildplate;
		}

		public static BuildplateShareResponse GetBuildplateById(BuildplateRequest buildplateReq)
		{
			BuildplateData buildplate = ReadBuildplate(buildplateReq.buildplateId);

			return new BuildplateShareResponse {result = new BuildplateShareResponse.BuildplateShareInfo {buildplateData = buildplate, playerId = null}};
		}

		public static void UpdateBuildplateAndList(BuildplateShareResponse data, string playerId)
		{
			data.result.buildplateData.eTag ??= "\"0xAAAAAAAAAAAAAAA\""; // TODO: If we ever use eTags for buildplates, replace this
			WriteBuildplate(data);

			var list = ReadPlayerBuildplateList(playerId);
			PlayerBuildplateList newList = new PlayerBuildplateList();
			for (int i = list.UnlockedBuildplates.IndexOf(data.result.buildplateData.id); i > 0; i--)
			{
				list.UnlockedBuildplates[i] = list.UnlockedBuildplates[i - 1];
			}

			list.UnlockedBuildplates[0] = data.result.buildplateData.id;

			WritePlayerBuildplateList(playerId, list);
		}

		public static ShareBuildplateResponse ShareBuildplate(Guid buildplateId, string playerId)
		{
			string sharedId = version4Generator.NewUuid().ToString();
			BuildplateData originalBuildplate = ReadBuildplate(buildplateId);
			SharedBuildplateData sharedBuildplate = new SharedBuildplateData() {
				blocksPerMeter = originalBuildplate.blocksPerMeter,
				dimension = originalBuildplate.dimension,
				model = originalBuildplate.model,
				offset = originalBuildplate.offset,
				order = originalBuildplate.order,
				surfaceOrientation = originalBuildplate.surfaceOrientation,
				type = "Survival"
			};

			InventoryResponse.Result inventory = InventoryUtils.GetHotbarForSharing(playerId);

			SharedBuildplateInfo buildplateInfo = new SharedBuildplateInfo() { playerId = "Unknown user", buildplateData = sharedBuildplate, inventory = inventory, sharedOn = DateTime.UtcNow};
			SharedBuildplateResponse buildplateResponse = new SharedBuildplateResponse() { result = buildplateInfo, continuationToken = null, expiration = null, updates = new Models.Updates() };
			
			WriteSharedBuildplate(buildplateResponse, sharedId);
			
			ShareBuildplateResponse response = new ShareBuildplateResponse() { result = "minecraftearth://sharedbuildplate?id=" + sharedId, expiration = null, continuationToken = null, updates = null};
			
			return response;
		}	

		public static SharedBuildplateResponse ReadSharedBuildplate(string buildplateId)
		{
			var filepath = StateSingleton.Instance.config.sharedBuildplateStorageFolderLocation + $"{buildplateId}.json"; 
			if (!File.Exists(filepath))
			{
				Log.Error($"Error: Tried to read buildplate that does not exist! BuildplateID: {buildplateId}");
				return null;
			}

			var buildplateJson = File.ReadAllText(filepath);
			var parsedobj = JsonConvert.DeserializeObject<SharedBuildplateResponse>(buildplateJson);
			return parsedobj;
		}

		public static void WriteSharedBuildplate(SharedBuildplateResponse data, string buildplateId)
		{
			var filepath = StateSingleton.Instance.config.sharedBuildplateStorageFolderLocation + $"{buildplateId}.json"; 

			File.WriteAllText(filepath, JsonConvert.SerializeObject(data));
		}

		public static BuildplateData ReadBuildplate(Guid buildplateId)
		{
			var filepath = StateSingleton.Instance.config.buildplateStorageFolderLocation + $"{buildplateId}.json"; 
			if (!File.Exists(filepath))
			{
				Log.Error($"Error: Tried to read buildplate that does not exist! BuildplateID: {buildplateId}");
				return null;
			}

			var buildplateJson = File.ReadAllText(filepath);
			var parsedobj = JsonConvert.DeserializeObject<BuildplateData>(buildplateJson);
			return parsedobj;
		}

		public static void WriteBuildplate(BuildplateData data)
		{
			var buildplateId = data.id;
			var filepath = StateSingleton.Instance.config.buildplateStorageFolderLocation + $"{buildplateId}.json"; 

			data.lastUpdated = DateTime.UtcNow;

			File.WriteAllText(filepath, JsonConvert.SerializeObject(data));
		}

		public static void WriteBuildplate(BuildplateShareResponse shareResponse)
			=> WriteBuildplate(shareResponse.result.buildplateData);
	}
}
