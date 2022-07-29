using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace ProjectEarthServerAPI.Models.Features
{
	public class Recipes
	{
		public Result result { get; set; }

		public static Recipes FromFile(string filepath)
		{
			var recipetext = File.ReadAllText(filepath);
			return JsonConvert.DeserializeObject<Recipes>(recipetext);
		}
	}

	public class Result
	{
		public List<Recipe> crafting { get; set; }
		public List<SmeltingRecipe> smelting { get; set; }
	}

	public class Recipe
	{
		public string category { get; set; }
		public RecipeIngredients[] ingredients { get; set; }
		public DateTime duration { get; set; } // HH:MM:SS
		public string id { get; set; }
		public RecipeOutput output { get; set; }
		public bool deprecated { get; set; }
		public ReturnItem[] returnItems { get; set; }
	}

	public class SmeltingRecipe
	{
		public Guid inputItemId { get; set; }
		public int heatRequired { get; set; }
		public string id { get; set; }
		public RecipeOutput output { get; set; }
		public bool deprecated { get; set; }
		public ReturnItem[] returnItems { get; set; }
	}

	public class RecipeIngredients
	{
		public int quantity { get; set; }
		public Guid[] items { get; set; }
	}

	public class RecipeOutput
	{
		public Guid itemId { get; set; }
		public int quantity { get; set; }
	}

	public class ReturnItem
	{
		public Guid id { get; set; }
		public int amount { get; set; }
	}
}
