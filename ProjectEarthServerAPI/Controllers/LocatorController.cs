using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using ProjectEarthServerAPI.Models;
using ProjectEarthServerAPI.Util;

namespace ProjectEarthServerAPI.Controllers
{
	[ApiController]
	[ApiVersion("1.0")]
	[ApiVersion("1.1")]
	[Route("player/environment")]
	public class LocatorController : Controller
	{
		private readonly ILogger<LocatorController> _logger;

		public LocatorController(ILogger<LocatorController> logger)
		{
			_logger = logger;
		}

		[HttpGet]
		public ContentResult Get()
		{
			string baseServerIP = StateSingleton.Instance.config.baseServerIP;

			LocatorResponse.Root response = new LocatorResponse.Root()
			{
				result = new LocatorResponse.Result()
				{
					serviceEnvironments = new LocatorResponse.ServiceEnvironments()
					{
						production = new LocatorResponse.Production()
						{
							playfabTitleId = StateSingleton.Instance.config.playfabTitleId,
							serviceUri = baseServerIP,
							cdnUri = baseServerIP + "/cdn",
							//playfabTitleId = "F0DE2" //maybe make our own soon? - Mojang could kill this anytime after server sunset with no warning. 
						}
					},
					supportedEnvironments = new Dictionary<string, List<string>>() {{"2020.1217.02", new List<string>() {"production"}}}
				},
				//updates = new LocatorResponse.Updates()
			};

			var resp = JsonConvert.SerializeObject(response);
			return Content(resp, "application/json");
		}
	}

	[ApiController]
	[ApiVersion("1.0")]
	[ApiVersion("1.1")]
	[Route("/api/v1.1/player/environment")]
	//:mojanktriggered:
	// Basically, the MCE gods decided peace was not an option
	// so for some reason, some devices include the /api/v1.1, and some don't.
	//  Hence this terrible thing.
	public class MojankLocatorController : Controller
	{
		private readonly ILogger<MojankLocatorController> _logger;

		public MojankLocatorController(ILogger<MojankLocatorController> logger)
		{
			_logger = logger;
		}

		[HttpGet]
		public ContentResult Get()
		{
			string baseServerIP = StateSingleton.Instance.config.baseServerIP;

			LocatorResponse.Root response = new LocatorResponse.Root()
			{
				result = new LocatorResponse.Result()
				{
					serviceEnvironments = new LocatorResponse.ServiceEnvironments()
					{
						production = new LocatorResponse.Production()
						{
							playfabTitleId = StateSingleton.Instance.config.playfabTitleId, serviceUri = baseServerIP, cdnUri = baseServerIP + "/cdn",
							//playfabTitleId = "F0DE2" //maybe make our own soon? - Mojang could kill this anytime after server sunset with no warning. 
						}
					},
					supportedEnvironments = new Dictionary<string, List<string>>() {{"2020.1217.02", new List<string>() {"production"}}}
				},
				//updates = new LocatorResponse.Updates()
			};

			var resp = JsonConvert.SerializeObject(response);
			return Content(resp, "application/json");
		}
	}
}
