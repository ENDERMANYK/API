using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace ProjectEarthServerAPI.Util
{
	/// <summary>
	/// Contains Functions for converting long/lat -> tile pos, downloading tilees, and anything else that might come up
	/// </summary>
	public class Tile
	{
		public static bool DownloadTile(int pos1, int pos2, string basePath)
		{
			WebClient webClient = new WebClient();

			try
			{
				Directory.CreateDirectory(Path.Combine(basePath, pos1.ToString()));
				//string downloadUrl = "https://cdn.mceserv.net/tile/16/" + pos1 + "/" + pos1 + "_" + pos2 + "_16.png";// Disabled because the server is down 
				string downloadUrl = StateSingleton.Instance.config.tileServerUrl + "/styles/mc-earth/16/" + pos1 + "/" + pos2 + ".png"; 
				webClient.DownloadFile(downloadUrl, Path.Combine(basePath, pos1.ToString(), $"{pos1}_{pos2}_16.png"));
				webClient.Dispose();
				return true;
			}
			catch (WebException wex)
			{
				//TODO: error 502 check.
				webClient.Dispose();
				return false;
			}
		}

		//From https://wiki.openstreetmap.org/wiki/Slippy_map_tilenames with slight changes

		public static string getTileForCoordinates(double lat, double lon)
		{
			//Adapted from java example. Zoom is replaced by the constant 16 because all MCE tiles are at zoom 16

			int xtile = (int)Math.Floor((lon + 180) / 360 * (1 << 16));
			int ytile = (int)Math.Floor((1 - Math.Log(Math.Tan(toRadians(lat)) + 1 / Math.Cos(toRadians(lat))) / Math.PI) / 2 * (1 << 16));

			if (xtile < 0)
				xtile = 0;
			if (xtile >= (1 << 16))
				xtile = ((1 << 16) - 1);
			if (ytile < 0)
				ytile = 0;
			if (ytile >= (1 << 16))
				ytile = ((1 << 16) - 1);

			return $"{xtile}_{ytile}";
		}

		//Helper
		static double toRadians(double angle)
		{
			return (Math.PI / 180) * angle;
		}
	}
}
