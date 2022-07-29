using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using ProjectEarthServerAPI.Models.Buildplate;
using ProjectEarthServerAPI.Models.Multiplayer;
using ProjectEarthServerAPI.Util;
using Serilog;
using ProjectEarthServerAPI.Models.Multiplayer.Adventure;

namespace ProjectEarthServerAPI.Controllers
{
	public class MultiplayerController : Controller
	{
		#region Buildplates

		[Authorize]
		[ApiVersion("1.1")]
		[Route("1/api/v{version:apiVersion}/multiplayer/buildplate/{buildplateId}/instances")]
		public async Task<IActionResult> PostCreateInstance(string buildplateId)
		{
			string authtoken = User.FindFirstValue(ClaimTypes.NameIdentifier);
			var stream = new StreamReader(Request.Body);
			var body = await stream.ReadToEndAsync();
			var parsedRequest = JsonConvert.DeserializeObject<BuildplateServerRequest>(body);

			var response = await MultiplayerUtils.CreateBuildplateInstance(authtoken, buildplateId, parsedRequest.playerCoordinate);
			return Content(JsonConvert.SerializeObject(response), "application/json");
		}

		[Authorize]
		[ApiVersion("1.1")]
		[Route("1/api/v{version:apiVersion}/multiplayer/buildplate/{buildplateId}/play/instances")]
		public async Task<IActionResult> PostCreatePlayInstance(string buildplateId)
		{
			string authtoken = User.FindFirstValue(ClaimTypes.NameIdentifier);
			var stream = new StreamReader(Request.Body);
			var body = await stream.ReadToEndAsync();
			var parsedRequest = JsonConvert.DeserializeObject<BuildplateServerRequest>(body);

			var response = await MultiplayerUtils.CreateBuildplateInstance(authtoken, buildplateId, parsedRequest.playerCoordinate);
			return Content(JsonConvert.SerializeObject(response), "application/json");
		}

		[Authorize]
		[ApiVersion("1.1")]
		[Route("1/api/v{version:apiVersion}/buildplates")]
		public IActionResult GetBuildplates()
		{
			string authtoken = User.FindFirstValue(ClaimTypes.NameIdentifier);

			var response = BuildplateUtils.GetBuildplatesList(authtoken);
			return Content(JsonConvert.SerializeObject(response), "application/json");
		}

		[Authorize]
		[ApiVersion("1.1")]
		[Route("1/api/v{version:apiVersion}/buildplates/{buildplateId}/share")]
		public IActionResult ShareBuildplate(string buildplateId)
		{
			string authtoken = User.FindFirstValue(ClaimTypes.NameIdentifier);
			var response = BuildplateUtils.ShareBuildplate(Guid.Parse(buildplateId), authtoken);
			return Content(JsonConvert.SerializeObject(response), "application/json");
		}

		[Authorize]
		[ApiVersion("1.1")]
		[Route("1/api/v{version:apiVersion}/buildplates/shared/{buildplateId}")]
		public IActionResult GetSharedBuildplate(string buildplateId)
		{
			string authtoken = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var response = BuildplateUtils.ReadSharedBuildplate(buildplateId);
			return Content(JsonConvert.SerializeObject(response), "application/json");
		}

		[Authorize]
		[ApiVersion("1.1")]
		[Route("1/api/v{version:apiVersion}/multiplayer/buildplate/shared/{buildplateId}/play/instances")]
		public async Task<IActionResult> PostSharedBuildplateCreatePlayInstance(string buildplateId)
		{
			string authtoken = User.FindFirstValue(ClaimTypes.NameIdentifier);
			var stream = new StreamReader(Request.Body);
			var body = await stream.ReadToEndAsync();
			var parsedRequest = JsonConvert.DeserializeObject<SharedBuildplateServerRequest>(body);
			
			var response = await MultiplayerUtils.CreateBuildplateInstance(authtoken, buildplateId, parsedRequest.playerCoordinate);
			return Content(JsonConvert.SerializeObject(response), "application/json");
		}

		[Authorize]
		[ApiVersion("1.1")]
		[Route("1/api/v{version:apiVersion}/multiplayer/join/instances")]
		public async Task<IActionResult> PostMultiplayerJoinInstance()
		{
			string authtoken = User.FindFirstValue(ClaimTypes.NameIdentifier);
			var stream = new StreamReader(Request.Body);
			var body = await stream.ReadToEndAsync();
			var parsedRequest = JsonConvert.DeserializeObject<MultiplayerJoinRequest>(body);
			Log.Information($"[{authtoken}]: Trying to join buildplate instance: id {parsedRequest.id}");
			
			var response = MultiplayerUtils.GetServerInstance(parsedRequest.id);
			return Content(JsonConvert.SerializeObject(response), "application/json");
		}

		#endregion

		[Authorize]
		[ApiVersion("1.1")]
		[Route("1/api/v{version:apiVersion}/multiplayer/encounters/{adventureid}/instances")]
		public async Task<IActionResult> PostCreateEncounterInstance(string adventureid)
		{
			string authtoken = User.FindFirstValue(ClaimTypes.NameIdentifier);
			var stream = new StreamReader(Request.Body);
			var body = await stream.ReadToEndAsync();
			var parsedRequest = JsonConvert.DeserializeObject<EncounterServerRequest>(body);

			var response = await MultiplayerUtils.CreateBuildplateInstance(authtoken, adventureid, parsedRequest.playerCoordinate);
			return Content(JsonConvert.SerializeObject(response), "application/json");
		}

		[Authorize]
		[ApiVersion("1.1")]
		[Route("1/api/v{version:apiVersion}/multiplayer/adventures/{adventureid}/instances")]
		public async Task<IActionResult> PostCreateAdventureInstance(string adventureid)
		{
			string authtoken = User.FindFirstValue(ClaimTypes.NameIdentifier);
			var stream = new StreamReader(Request.Body);
			var body = await stream.ReadToEndAsync();
			var parsedRequest = JsonConvert.DeserializeObject<BuildplateServerRequest>(body);

			var response = await MultiplayerUtils.CreateBuildplateInstance(authtoken, adventureid, parsedRequest.playerCoordinate);
			return Content(JsonConvert.SerializeObject(response), "application/json");
		}

		[Authorize]
		[ApiVersion("1.1")]
		[Route("1/api/v{version:apiVersion}/multiplayer/encounters/state")]
		public async Task<IActionResult> EncounterState()
		{
			string authtoken = User.FindFirstValue(ClaimTypes.NameIdentifier);
			var stream = new StreamReader(Request.Body);
			var body = await stream.ReadToEndAsync();
			var request = JsonConvert.DeserializeObject<Dictionary<Guid, string>>(body);
			var response = new EncounterStateResponse { result = new Dictionary<Guid, ActiveEncounterStateMetadata> { {Guid.Parse("b7335819-c123-49b9-83fb-8a0ec5032779"), new ActiveEncounterStateMetadata { ActiveEncounterState = ActiveEncounterState.Dirty}}}, expiration=null, continuationToken=null, updates= null };
			return Content(JsonConvert.SerializeObject(response), "application/json");
		}

		[Authorize]
		[ApiVersion("1.1")]
		[Route("1/api/v{version:apiVersion}/multiplayer/partitions/{worldId}/instances/{instanceId}")]
		public IActionResult GetInstanceStatus(string worldId, Guid instanceId)
		{
			string authtoken = User.FindFirstValue(ClaimTypes.NameIdentifier);

			var response = MultiplayerUtils.CheckInstanceStatus(authtoken, instanceId);
			if (response == null)
			{
				return StatusCode(204);
			}
			else
			{
				return Content(JsonConvert.SerializeObject(response), "application/json");
			}
		}

		[ApiVersion("1.1")]
		[Route("1/api/v{version:apiVersion}/private/server/command")]
		public async Task<IActionResult> PostServerCommand()
		{
			var stream = new StreamReader(Request.Body);
			var body = await stream.ReadToEndAsync();
			var parsedRequest = JsonConvert.DeserializeObject<ServerCommandRequest>(body);

			var response = MultiplayerUtils.ExecuteServerCommand(parsedRequest);

			if (response == "ok") return Ok();
			else return Content(response, "application/json");
		}

		[ApiVersion("1.1")]
		[Route("1/api/v{version:apiVersion}/private/server/ws")]
		public async Task GetWebSocketServer()
		{
			if (HttpContext.WebSockets.IsWebSocketRequest)
			{
				var webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();
				await MultiplayerUtils.AuthenticateServer(webSocket);
			}
		}
	}
}
