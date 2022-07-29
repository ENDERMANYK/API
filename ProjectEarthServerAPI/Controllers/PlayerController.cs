using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using System.IO;
using Newtonsoft.Json;
using ProjectEarthServerAPI.Models;
using ProjectEarthServerAPI.Models.Player;
using ProjectEarthServerAPI.Util;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Serilog;
using Uma.Uuid;
using System.Collections.Generic;

namespace ProjectEarthServerAPI.Controllers
{
    [Authorize]
    public class PlayerTokenController : Controller
    {
        [Authorize]
        [ApiVersion("1.1")]
        [ResponseCache(Duration = 11200)]
        [Route("1/api/v{version:apiVersion}/player/tokens")]
        public IActionResult Get()
        {
            string authtoken = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var returnTokens = TokenUtils.ReadTokens(authtoken);

			Log.Debug($"[{authtoken}]: Requested tokens."); // Debug since this is spammed a lot

            return Content(JsonConvert.SerializeObject(returnTokens), "application/json");
        }

        [ApiVersion("1.1")]
        [Route("1/api/v{version:apiVersion}/player/tokens/{token}/redeem")] // TODO: Proper testing
        public IActionResult RedeemToken(Guid token)
        {
            string authtoken = User.FindFirstValue(ClaimTypes.NameIdentifier);

			var redeemedToken = TokenUtils.RedeemToken(authtoken, token);

			return Content(JsonConvert.SerializeObject(redeemedToken), "application/json");
		}
	}

	[Authorize]
	[ApiVersion("1.1")]
	[ResponseCache(Duration = 11200)]
	[Route("1/api/v{version:apiVersion}/player/rubies")]
	public class PlayerRubiesController : Controller
	{
		public IActionResult Get()
		{
			var obj = RubyUtils.GetNormalRubyResponse(User.FindFirstValue(ClaimTypes.NameIdentifier));
			var response = JsonConvert.SerializeObject(obj);
			return Content(response, "application/json");
		}
	}

	[Authorize]
	[Route("1/api/v{version:apiVersion}/player/profile/language")] 
	public class PlayerLanguageController : Controller
	{
		public IActionResult Get(string language)
		{
			return Ok();
		}
	}

	[Authorize]
	[ApiVersion("1.1")]
	[ResponseCache(Duration = 11200)]
	[Route("1/api/v{version:apiVersion}/player/splitRubies")]
	public class PlayerSplitRubiesController : Controller
	{
		public IActionResult Get()
		{
			var obj = RubyUtils.ReadRubies(User.FindFirstValue(ClaimTypes.NameIdentifier));
			var response = JsonConvert.SerializeObject(obj);
			return Content(response, "application/json");
		}
	}

	[Authorize]
	public class PlayerChallengesController : Controller
	{
		[ApiVersion("1.1")]
		[Route("1/api/v{version:apiVersion}/player/challenges")]
		public IActionResult GetChallenges()
		{
			var authtoken = User.FindFirstValue(ClaimTypes.NameIdentifier);
			var challenges = ChallengeUtils.ReloadChallenges(authtoken);
			return Content(JsonConvert.SerializeObject(challenges), "application/json");
		}

		[ApiVersion("1.1")]
		[Route("1/api/v{version:apiVersion}/challenges/season/active/{challengeId}")]
		public IActionResult PutActivateChallenge(Guid challengeId)
		{
			var authtoken = User.FindFirstValue(ClaimTypes.NameIdentifier);
			var success = ChallengeUtils.ActivateChallengeForPlayer(authtoken, challengeId);

			if (success) return Ok();
			else return Unauthorized();
		}

		[ApiVersion("1.1")]
		[Route("1/api/v{version:apiVersion}/challenges/timed/generate")]
		public IActionResult PostGenerateTimedChallenges()
		{
			var authtoken = User.FindFirstValue(ClaimTypes.NameIdentifier);
			ChallengeUtils.GenerateTimedChallenges(authtoken);
			return Ok();
		}

		[ApiVersion("1.1")]
		[Route("1/api/v{version:apiVersion}/challenges/continuous/{challengeId}/remove")]
		public IActionResult DeleteContinuousChallenge(Guid challengeId)
		{
			var authtoken = User.FindFirstValue(ClaimTypes.NameIdentifier);
			ChallengeUtils.RemoveChallengeFromPlayer(authtoken, challengeId);
			return Ok();
		}
	}


	[Authorize]
	[ApiVersion("1.1")]
	[Route("1/api/v{version:apiVersion}/player/utilityBlocks")]
	public class PlayerUtilityBlocksController : Controller
	{
		public IActionResult Get()
		{
			var authtoken = User.FindFirstValue(ClaimTypes.NameIdentifier);
			var response = UtilityBlockUtils.ReadUtilityBlocks(authtoken);
			return Content(JsonConvert.SerializeObject(response), "application/json");
			//return Content("{\"result\":{\"crafting\":{\"1\":{\"sessionId\":null,\"recipeId\":null,\"output\":null,\"escrow\":[],\"completed\":0,\"available\":0,\"total\":0,\"nextCompletionUtc\":null,\"totalCompletionUtc\":null,\"state\":\"Empty\",\"boostState\":null,\"unlockPrice\":null,\"streamVersion\":4},\"2\":{\"sessionId\":null,\"recipeId\":null,\"output\":null,\"escrow\":[],\"completed\":0,\"available\":0,\"total\":0,\"nextCompletionUtc\":null,\"totalCompletionUtc\":null,\"state\":\"Locked\",\"boostState\":null,\"unlockPrice\":{\"cost\":1,\"discount\":0},\"streamVersion\":4},\"3\":{\"sessionId\":null,\"recipeId\":null,\"output\":null,\"escrow\":[],\"completed\":0,\"available\":0,\"total\":0,\"nextCompletionUtc\":null,\"totalCompletionUtc\":null,\"state\":\"Locked\",\"boostState\":null,\"unlockPrice\":{\"cost\":1,\"discount\":0},\"streamVersion\":4}},\"smelting\":{\"1\":{\"fuel\":null,\"burning\":null,\"hasSufficientFuel\":null,\"heatAppliedToCurrentItem\":null,\"sessionId\":null,\"recipeId\":null,\"output\":null,\"escrow\":[],\"completed\":0,\"available\":0,\"total\":0,\"nextCompletionUtc\":null,\"totalCompletionUtc\":null,\"state\":\"Empty\",\"boostState\":null,\"unlockPrice\":null,\"streamVersion\":4},\"2\":{\"fuel\":null,\"burning\":null,\"hasSufficientFuel\":null,\"heatAppliedToCurrentItem\":null,\"sessionId\":null,\"recipeId\":null,\"output\":null,\"escrow\":[],\"completed\":0,\"available\":0,\"total\":0,\"nextCompletionUtc\":null,\"totalCompletionUtc\":null,\"state\":\"Locked\",\"boostState\":null,\"unlockPrice\":{\"cost\":1,\"discount\":0},\"streamVersion\":4},\"3\":{\"fuel\":null,\"burning\":null,\"hasSufficientFuel\":null,\"heatAppliedToCurrentItem\":null,\"sessionId\":null,\"recipeId\":null,\"output\":null,\"escrow\":[],\"completed\":0,\"available\":0,\"total\":0,\"nextCompletionUtc\":null,\"totalCompletionUtc\":null,\"state\":\"Locked\",\"boostState\":null,\"unlockPrice\":{\"cost\":1,\"discount\":0},\"streamVersion\":4}}},\"expiration\":null,\"continuationToken\":null,\"updates\":{}}", "application/json");
		}
	}
	//
	// [Authorize]
	// [ApiVersion("1.1")]
	// [Route("1/api/v{version:apiVersion}/player/profile/{profileId}")]
	// public class PlayerProfileController : Controller
	// {
	//     public IActionResult Get(string profileId)
	//     {
	//         var response = new ProfileResponse(ProfileUtils.ReadProfile(profileId));
	//         return Content(JsonConvert.SerializeObject(response), "application/json");
	//     }
	// }


	[Authorize]
	public class PlayerInventoryController : Controller
	{
		[ApiVersion("1.1")]
		[Route("1/api/v{version:apiVersion}/inventory/catalogv3")]
		public IActionResult GetCatalog()
		{
			return Content(JsonConvert.SerializeObject(StateSingleton.Instance.catalog), "application/json");
		}

		[ApiVersion("1.1")]
		[Route("1/api/v{version:apiVersion}/inventory/survival")]
		public IActionResult GetSurvivalInventory()
		{
			var inv = InventoryUtils.ReadInventory(User.FindFirstValue(ClaimTypes.NameIdentifier));
			var jsonstring = JsonConvert.SerializeObject(inv);
			return Content(jsonstring, "application/json");
		}

		[ApiVersion("1.1")]
		[Route("1/api/v{version:apiVersion}/inventory/survival/hotbar")]
		public async Task<IActionResult> PutItemInHotbar()
		{
			var stream = new StreamReader(Request.Body);
			var body = await stream.ReadToEndAsync();

			string authtoken = User.FindFirstValue(ClaimTypes.NameIdentifier);
			var newHotbar = JsonConvert.DeserializeObject<InventoryResponse.Hotbar[]>(body);
			var returnHotbar = InventoryUtils.EditHotbar(authtoken, newHotbar);

			return Content(JsonConvert.SerializeObject(returnHotbar.Item2));
		}
	}

	[Authorize]
	[ApiVersion("1.1")]
	[Route("1/api/v{version:apiVersion}/settings")]
	public class PlayerSettingsController : Controller
	{
		public IActionResult Get()
		{
			return Content(JsonConvert.SerializeObject(StateSingleton.Instance.settings), "application/json");
		}
	} // TODO: Fixed String

	[Authorize]
	[ApiVersion("1.1")]
	[Route("1/api/v{version:apiVersion}/recipes")]
	public class PlayerRecipeController : Controller
	{
		public IActionResult Get()
		{
			var recipeString = System.IO.File.ReadAllText(StateSingleton.Instance.config.recipesFileLocation); // Since the serialized version has the properties mixed up
			return Content(recipeString, "application/json");
		}
	}

	[Authorize]
	[ApiVersion("1.1")]
	public class JournalController : Controller
	{
		[Route("1/api/v{version:apiVersion}/journal/catalog")]
		public IActionResult GetCatalog()
		{
			var fs = new StreamReader(StateSingleton.Instance.config.journalCatalogFileLocation);
			return Content(fs.ReadToEnd(), "application/json");
		}

		[Route("1/api/v{version:apiVersion}/player/journal")]
		public IActionResult GetJournal()
		{
			var authtoken = User.FindFirstValue(ClaimTypes.NameIdentifier);
			var resp = JournalUtils.ReadJournalForPlayer(authtoken);

			return Content(JsonConvert.SerializeObject(resp), "application/json");
		}
	}

	[Authorize]
	public class PlayerBoostController : Controller
	{
		[ApiVersion("1.1")]
		[Route("1/api/v{version:apiVersion}/boosts")]
		public IActionResult Get()
		{
			string authtoken = User.FindFirstValue(ClaimTypes.NameIdentifier);
			var responseobj = BoostUtils.UpdateBoosts(authtoken);
			var response = JsonConvert.SerializeObject(responseobj);
			return Content(response, "application/json");
		}

		[ApiVersion("1.1")]
		[Route("1/api/v{version:apiVersion}/boosts/potions/{boostId}/activate")]
		public IActionResult GetRedeemBoost(Guid boostId)
		{
			string authtoken = User.FindFirstValue(ClaimTypes.NameIdentifier);
			var returnUpdates = BoostUtils.ActivateBoost(authtoken, boostId);
			return Content(JsonConvert.SerializeObject(returnUpdates), "application/json");
		}

		[ApiVersion("1.1")]
		[Route("1/api/v{version:apiVersion}/boosts/potions/{boostInstanceId}/deactivate")]
		public IActionResult DeactivateBoost(string boostInstanceId) 
		{
			string authtoken = User.FindFirstValue(ClaimTypes.NameIdentifier);
			var returnUpdates = BoostUtils.RemoveBoost(authtoken, boostInstanceId);
			return Content(JsonConvert.SerializeObject(returnUpdates), "application/json");
		}

		[ApiVersion("1.1")]
		[Route("1/api/v{version:apiVersion}/boosts/{boostInstanceId}")]
		public IActionResult DeleteBoost(string boostInstanceId)
		{
			string authtoken = User.FindFirstValue(ClaimTypes.NameIdentifier);
			var returnUpdates = BoostUtils.RemoveBoost(authtoken, boostInstanceId);
			return Content(JsonConvert.SerializeObject(returnUpdates), "application/json");
		}
	} // TODO: In Progress

	[Authorize]
	[ApiVersion("1.1")]
	[Route("1/api/v{version:apiVersion}/features")]
	public class PlayerFeaturesController : Controller
	{
		public IActionResult Get()
		{
			var responseobj = new FeaturesResponse() { result = new FeaturesResult(), updates = new Updates() };
			var response = JsonConvert.SerializeObject(responseobj);
			return Content(response, "application/json");
		}
	} // TODO: Fixed String, just get from a JSON for serverwide settings

	[Authorize]
	public class PlayerShopController : Controller
	{
		[ApiVersion("1.1")]
		[Route("1/api/v{version:apiVersion}/products/catalog")]
		public IActionResult GetProductCatalog()
		{
			var catalog = System.IO.File.ReadAllText(StateSingleton.Instance.config.productCatalogFileLocation); // Since the serialized version has the properties mixed up
			return Content(catalog, "application/json");
		}
		[ApiVersion("1.1")]
		[Route("1/api/v{version:apiVersion}/commerce/storeItemInfo")]
		public async Task<IActionResult> GetStoreItemInfo()
		{
			var stream = new StreamReader(Request.Body);
			var body = await stream.ReadToEndAsync();
			var response = ShopUtils.getStoreItemInfo(JsonConvert.DeserializeObject<List<StoreItemInfo>>(body));
			return Content(JsonConvert.SerializeObject(response), "application/json");
		}
		[ApiVersion("1.1")]
		[Route("1/api/v{version:apiVersion}/commerce/purchase")]
		public async Task<IActionResult> ItemPurchase()
		{
			var stream = new StreamReader(Request.Body);
			var body = await stream.ReadToEndAsync();
			var response = ShopUtils.purchase(User.FindFirstValue(ClaimTypes.NameIdentifier), JsonConvert.DeserializeObject<PurchaseItemRequest>(body));
			return Content(JsonConvert.SerializeObject(response), "application/json");
		}
		[ApiVersion("1.1")]
		[Route("1/api/v{version:apiVersion}/commerce/purchaseV2")]
		public async Task<IActionResult> ItemPurchaseV2()
		{
			var stream = new StreamReader(Request.Body);
			var body = await stream.ReadToEndAsync();
			var response = ShopUtils.purchaseV2(User.FindFirstValue(ClaimTypes.NameIdentifier), JsonConvert.DeserializeObject<PurchaseItemRequest>(body));
			return Content(JsonConvert.SerializeObject(response), "application/json");
		}
	} // TODO: Needs Playfab counterpart. When that is in place we can implement buildplate previews.
}
