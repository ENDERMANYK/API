using System;
using System.Buffers.Text;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using ProjectEarthServerAPI.Models;
using ProjectEarthServerAPI.Models.Buildplate;
using ProjectEarthServerAPI.Models.Features;
using ProjectEarthServerAPI.Models.Multiplayer;
using ProjectEarthServerAPI.Models.Player;
using Serilog;
using Uma.Uuid;

namespace ProjectEarthServerAPI.Util
{
	public class MultiplayerUtils
	{
		private static readonly Dictionary<Guid, BuildplateServerResponse> InstanceList = new();
		private static readonly Dictionary<Guid, Guid> ApiKeyList = new();
		private static readonly Dictionary<Guid, ServerInformation> ServerInfoList = new();
		private static readonly Dictionary<Guid, WebSocket> ServerSocketList = new();
		private static readonly Dictionary<Guid, bool> InstanceReadyList = new();

		public static async Task<BuildplateServerResponse> CreateBuildplateInstance(string playerId,
			string buildplateId,
			Coordinate playerCoords)
		{

			Log.Information($"[{playerId}]: Creating new buildplate instance: Buildplate {buildplateId}");

			Random rdm = new Random();
			var serverRoleInstanceBytes = new byte[6];
			rdm.NextBytes(serverRoleInstanceBytes);

			//var serverRoleInstance = BitConverter.ToString(serverRoleInstanceBytes);
			var serverRoleInstance = "776932eeeb69";
			var serverPlayerJoinCode = Convert.ToBase64String(serverRoleInstanceBytes);

			var server = ServerInfoList.First();
			var serverIp = server.Value.ip;
			var serverPort = server.Value.port;
			var serverInstanceId = await NotifyServerInstance(server.Key, buildplateId, playerId);

			var buildplate = BuildplateUtils.ReadBuildplate(Guid.Parse(buildplateId));
			var blocksPerMeter = buildplate.blocksPerMeter;
			var buildplateOffset = buildplate.offset;
			var instanceMetadata = new BuildplateServerResponse.InstanceMetadata {buildplateid = buildplateId};

			var dimensions = buildplate.dimension;
			var templateId = buildplate.templateId; // Not used AFAIK
			var surfaceOrientation = buildplate.surfaceOrientation; // Can also be vertical

			var buildplateData = new BuildplateServerResponse.GameplayMetadata
			{
				augmentedImageSetId = null,
				blocksPerMeter = blocksPerMeter,
				breakableItemToItemLootMap = new BuildplateServerResponse.BreakableItemToItemLootMap(),
				dimension = dimensions,
				gameplayMode = GameplayMode.Buildplate,
				isFullSize =
					false, // TODO: Defines if the buildplate should be rendered, we just disable it (Actual Check: (dimensions.x >= 32 && dimensions.z >= 32))
				offset = buildplateOffset, // Same for all buildplates
				playerJoinCode = serverPlayerJoinCode, // 24 letters/Numbers, probably randomly generated
				rarity = null, // Why even is this here?
				shutdownBehavior = new List<string>()
				{
					"ServerShutdownWhenAllPlayersQuit", "ServerShutdownWhenHostPlayerQuits"
				}, // Own instance server needs to respect this
				snapshotOptions = new BuildplateServerResponse.SnapshotOptions
				{
					saveState = new BuildplateServerResponse.SaveState // Should be the same for all buildplates
					{
						inventory = true, model = true, world = true
					},
					snapshotTriggerConditions = "None",
					snapshotWorldStorage = "Buildplate",
					triggerConditions = new List<string>() {"Interval", "PlayerExits"},
					triggerInterval = new TimeSpan(00, 00, 30)
				},
				spawningClientBuildNumber =
					"2020.1217.02", // How should we figure this out? Should probably just be the latest every time
				spawningPlayerId = playerId,
				surfaceOrientation = surfaceOrientation,
				templateId = templateId,
				worldId = buildplateId
			};

			var result = new BuildplateServerResponse
			{
				result = new BuildplateServerResponse.Result
				{
					applicationStatus = "Unknown",
					//fqdn = "dns2527870c-89c6-420e-8378-996a2c40304a-azurebatch-cloudservice.westeurope.cloudapp.azure.com", // figure out why this breaks everything
					fqdn = "d.projectearth.dev",
					gameplayMetadata = buildplateData,
					hostCoordinate = playerCoords,
					instanceId = serverInstanceId.ToString(),
					ipV4Address = serverIp,
					metadata = JsonConvert.SerializeObject(instanceMetadata),
					partitionId = playerId,
					port = serverPort,
					roleInstance = serverRoleInstance,
					serverReady = false,
					serverStatus = "Running"
				},
				updates = new Updates()
			};

			if (InstanceReadyList[serverInstanceId])
			{
				result.result.applicationStatus = "Ready";
				result.result.serverReady = true;
			}

			InstanceList.Add(serverInstanceId, result);

			return result;
		}

		public static async Task<Guid> NotifyServerInstance(Guid serverId, string buildplateId, string playerId)
		{
			var instanceId = Guid.NewGuid();

			InstanceReadyList.Add(instanceId, false);

			ServerInstanceRequestInfo instanceInfo = new ServerInstanceRequestInfo {buildplateId = Guid.Parse(buildplateId), instanceId = instanceId, playerId = playerId};

			string requestString = JsonConvert.SerializeObject(instanceInfo);
			byte[] requestArr = Encoding.UTF8.GetBytes(requestString);

			await ServerSocketList[serverId].SendAsync(new ArraySegment<byte>(requestArr, 0, requestArr.Length),
				WebSocketMessageType.Text, true, CancellationToken.None);

			return instanceId;
		}

		public static BuildplateServerResponse CheckInstanceStatus(string playerId, Guid instanceId)
		{
			if (InstanceReadyList[instanceId])
			{
				InstanceList[instanceId].result.applicationStatus = "Ready";
				InstanceList[instanceId].result.serverReady = true;
				return InstanceList[instanceId];
			}
			else return InstanceList[instanceId];
		}

		public static BuildplateServerResponse GetServerInstance(string joinCode)
		{
			var instance = InstanceList.FirstOrDefault(match => match.Value.result.gameplayMetadata.playerJoinCode == joinCode).Value;
			return instance;
		}

		private static HotbarTranslation[] EditHotbarForPlayer(string playerId, MultiplayerItem[] multiplayerHotbar)
		{
			if (multiplayerHotbar == null)
			{
				return null;
			}

			if (multiplayerHotbar.Length != 7)
			{
				var tempArr = new MultiplayerItem[7];
				multiplayerHotbar.CopyTo(tempArr, 0);
				for (int i = 0; i < tempArr.Length; i++)
				{
					tempArr[i] ??= new MultiplayerItem
					{
						category = new MultiplayerItemCategory
						{
							loc = ItemCategory.Invalid,
							value = (int)ItemCategory.Invalid
						},
						count = 0,
						guid = Guid.Empty,
						owned = true,
						rarity = new MultiplayerItemRarity
						{
							loc = ItemRarity.Invalid,
							value = (int)ItemRarity.Invalid
						}
					};
				}

				multiplayerHotbar = tempArr;
			}

			var inv = InventoryUtils.ReadInventory(playerId);
			var hotbar = new InventoryResponse.Hotbar[multiplayerHotbar.Length];
			HotbarTranslation[] response = new HotbarTranslation[multiplayerHotbar.Length];

			for (int i = 0; i < multiplayerHotbar.Length; i++)
			{
				MultiplayerItem item = multiplayerHotbar[i];
				if (item.guid != Guid.Empty)
				{
					var catalogItem = StateSingleton.Instance.catalog.result.items.Find(match => match.id == item.guid);
					if (item.instance_data != null)
					{
						hotbar[i] = new InventoryResponse.Hotbar
						{
							count = 1,
							id = item.guid,
							instanceId = item.instance_data.id,
							health = item.instance_data.health
						};
					}
					else
					{
						hotbar[i] = new InventoryResponse.Hotbar
						{
							count = item.count,
							id = item.guid,
							instanceId = null,
							health = null
						};
					}

					response[i] = new HotbarTranslation
					{
						count = item.count,
						identifier = catalogItem.item.name,
						meta = catalogItem.item.aux,
						slotId = i
					};
				}
				else
				{
					hotbar[i] = null;
					response[i] = new HotbarTranslation
					{
						count = 0,
						identifier = "air",
						meta = 0,
						slotId = i
					};
				}
			}

			InventoryUtils.EditHotbar(playerId, hotbar);

			return response;
		}

		private static HotbarTranslation[] GetHotbarForPlayer(string playerId)
		{
			var inv = InventoryUtils.ReadInventory(playerId);
			var hotbar = inv.result.hotbar;
			HotbarTranslation[] response = new HotbarTranslation[hotbar.Length];

			for (int i = 0; i < hotbar.Length; i++)
			{
				InventoryResponse.Hotbar item = hotbar[i];

				if (item != null)
				{
					var catalogItem = StateSingleton.Instance.catalog.result.items.Find(match => match.id == item.id);

					response[i] = new HotbarTranslation {count = item.count, identifier = catalogItem.item.name, meta = catalogItem.item.aux, slotId = i};
				}
				else
				{
					response[i] = new HotbarTranslation {count = 0, identifier = "air", meta = 0, slotId = i};
				}
			}

			return response;
		}

		public static void EditInventoryForPlayer(string playerId, EditInventoryRequest request)
		{
			var damage = request.meta == -1 ? 0 : request.meta;
			var catalogItem =
				StateSingleton.Instance.catalog.result.items.Find(match =>
					match.item.name == request.identifier && match.item.aux == damage);
			var isNonStackableItem = catalogItem.item.type == "Tool";
			var isHotbar = request.slotIndex <= 6;

			if (isHotbar)
			{
				var hotbar = InventoryUtils.GetHotbar(playerId).Item2;
				var slot = hotbar[request.slotIndex] ?? new InventoryResponse.Hotbar();

				if (request.removeItem) slot = null;
				else
				{
					slot.count = request.count + 1;
					slot.id = catalogItem.id;

					if (isNonStackableItem) slot.health = request.health;
				}

				hotbar[request.slotIndex] = slot;
				InventoryUtils.EditHotbar(playerId, hotbar, false);
			}
			else
			{
				// Removing items from the normal inventory should never be possible, except from the hotbar
				//if (request.removeItem) InventoryUtils.RemoveItemFromInv(playerId, catalogItem.id, request.count, request.health);
				//else 
				InventoryUtils.AddItemToInv(playerId, catalogItem.id, request.count);
			}
		}
		public static string ExecuteServerCommand(ServerCommandRequest request)
		{
			var command = request.command;
			Log.Information($"Received {command} from Server {request.serverId}!");
			if (ApiKeyList.ContainsValue(request.apiKey))
			{
				var playerId = request.playerId;
				switch (command)
				{
					case ServerCommandType.GetBuildplate:
						var buildplate = JsonConvert.DeserializeObject<BuildplateRequest>(request.requestData);
						return JsonConvert.SerializeObject(BuildplateUtils.GetBuildplateById(buildplate));

					case ServerCommandType.GetInventoryForClient:
						var inv = InventoryUtils.ReadInventoryForMultiplayer(playerId);
						return JsonConvert.SerializeObject(inv);

					case ServerCommandType.GetInventory:
						var hotbarForServer = GetHotbarForPlayer(playerId);
						return JsonConvert.SerializeObject(hotbarForServer);

					case ServerCommandType.EditInventory:
						var invEdits = JsonConvert.DeserializeObject<EditInventoryRequest>(request.requestData);
						EditInventoryForPlayer(playerId, invEdits);
						return "ok";

					case ServerCommandType.EditHotbar:
						var invData = JsonConvert.DeserializeObject<MultiplayerInventoryResponse>(request.requestData);
						var newHotbarInfo = EditHotbarForPlayer(playerId, invData.hotbar);
						return JsonConvert.SerializeObject(newHotbarInfo);

					case ServerCommandType.EditBuildplate:
						var newBuildplateData =
							JsonConvert.DeserializeObject<BuildplateShareResponse>(request.requestData);
						//BuildplateUtils.UpdateBuildplateAndList(newBuildplateData, playerId);
						return "ok";

					case ServerCommandType.MarkServerAsReady:
						var instanceInfo = JsonConvert.DeserializeObject<ServerInstanceInfo>(request.requestData);
						MarkServerAsReady(instanceInfo);
						return "ok";

					default:
						return null;
				}
			}
			else return null;
		}

		public static void MarkServerAsReady(ServerInstanceInfo info)
		{
			InstanceReadyList[info.instanceId] = true;
		}

		public static async Task AuthenticateServer(WebSocket webSocketRequest)
		{
			byte[] messageBuffer = new byte[4096];
			var result =
				await webSocketRequest.ReceiveAsync(new ArraySegment<byte>(messageBuffer), CancellationToken.None);
			var authStatus = ServerAuthInformation.NotAuthed;
			ServerInformation info = null;
			string challenge = null;

			while (!result.CloseStatus.HasValue)
			{
				info ??= JsonConvert.DeserializeObject<ServerInformation>(Encoding.UTF8.GetString(messageBuffer));

				switch (authStatus)
				{
					case ServerAuthInformation.NotAuthed: // Send Challenge

						challenge = Guid.NewGuid().ToString();
						byte[] challengeBytes = Encoding.UTF8.GetBytes(challenge);

						await webSocketRequest.SendAsync(
							new ArraySegment<byte>(challengeBytes, 0, challengeBytes.Length), result.MessageType,
							result.EndOfMessage, CancellationToken.None);

						authStatus = ServerAuthInformation.AuthStage1;

						Array.Clear(messageBuffer, 0, messageBuffer.Length);
						result = await webSocketRequest.ReceiveAsync(new ArraySegment<byte>(messageBuffer),
							CancellationToken.None);

						break;

					case ServerAuthInformation.AuthStage1: // Verify challenge response
						string challengeResponse = Encoding.UTF8.GetString(messageBuffer).TrimEnd('\0');
						var success = VerifyChallenge(challenge, challengeResponse, info);

						byte[] challengeResponseStatus = Encoding.UTF8.GetBytes(success.ToString().ToLower());

						await webSocketRequest.SendAsync(
							new ArraySegment<byte>(challengeResponseStatus, 0, challengeResponseStatus.Length),
							result.MessageType, result.EndOfMessage, CancellationToken.None);

						if (success) authStatus = ServerAuthInformation.AuthStage2;
						else
						{
							authStatus = ServerAuthInformation.FailedAuth;

							await webSocketRequest.CloseAsync(WebSocketCloseStatus.Empty, null, CancellationToken.None);

							return;
						}

						break;

					case ServerAuthInformation.AuthStage2:

						if (!ApiKeyList.ContainsKey(info.serverId))
						{
							Log.Information($"Server {info.serverId} registered itself to the api.");

							var apiKey = Guid.NewGuid();
							ApiKeyList.Add(info.serverId, apiKey);
							ServerInfoList.Add(info.serverId, info);
							ServerSocketList.Add(info.serverId, webSocketRequest);

							byte[] apiKeyBytes = Encoding.UTF8.GetBytes(apiKey.ToString().ToLower());

							await webSocketRequest.SendAsync(
								new ArraySegment<byte>(apiKeyBytes, 0, apiKeyBytes.Length), result.MessageType,
								result.EndOfMessage, CancellationToken.None);

							authStatus = ServerAuthInformation.Authed;
						}

						break;

					case ServerAuthInformation.Authed:
						while (true) { }

						break;
				}
			}
		}

		private static bool VerifyChallenge(string challenge, string challengeResponse, ServerInformation info)
		{
			HMACSHA256 crypto = new HMACSHA256 {Key = Convert.FromBase64String(StateSingleton.Instance.config.multiplayerAuthKeys[info.ip])};

			var expectedResult = Convert.ToHexString(crypto.ComputeHash(Encoding.UTF8.GetBytes(challenge)));
			return expectedResult == challengeResponse;
		}
	}
}
