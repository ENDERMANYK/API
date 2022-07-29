using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using ProjectEarthServerAPI.Models;
using ProjectEarthServerAPI.Models.Player;
using Serilog;

namespace ProjectEarthServerAPI.Util 
{
    public class ShopUtils 
    {
        public static Dictionary<Guid, StoreItemInfo> readShopItemDictionary() {
            var filepath = StateSingleton.Instance.config.ShopItemDictionaryFileLocation;
            var storeItemsJson = File.ReadAllText(filepath);
            return JsonConvert.DeserializeObject<Dictionary<Guid, StoreItemInfo>>(storeItemsJson);
        }

        public static void processPurchase(string playerId, PurchaseItemRequest request) {
            try {
                var shopItems = readShopItemDictionary();
                var itemToPurchase = shopItems[request.itemId];
                if (itemToPurchase.storeItemType == StoreItemType.Items) {
                    foreach (var item in itemToPurchase.inventoryCounts) {
                        InventoryUtils.AddItemToInv(playerId, item.Key, item.Value);
                    }
                } else {
                    BuildplateUtils.AddToPlayer(playerId, itemToPurchase.id);
                }
                RubyUtils.RemoveRubiesFromPlayer(playerId, request.expectedPurchasePrice);
            }
            catch {   
                Log.Error("Error: Failed to process shop order for item id: " + request.itemId);
                return;
            }
        }

        public static RubyResponse purchase(string playerId, PurchaseItemRequest request) {
            processPurchase(playerId, request);
            return RubyUtils.GetNormalRubyResponse(playerId);
        }

        public static SplitRubyResponse purchaseV2(string playerId, PurchaseItemRequest request) {
            processPurchase(playerId, request);
            return RubyUtils.ReadRubies(playerId);
        }

        public static StoreItemInfoResponse getStoreItemInfo(List<StoreItemInfo> request) {
            var result = new List<StoreItemInfo>();
            var shopItems = readShopItemDictionary();
            for (int i = 0; i < request.Count; i++) {
                if (request[i].storeItemType == StoreItemType.Buildplates) {
                    var buildplate = BuildplateUtils.ReadBuildplate(request[i].id);
                    var itemFromMap = shopItems.FirstOrDefault(match => match.Value.id == request[i].id).Value;
                    if (buildplate != null) {
                        result.Add(new StoreItemInfo() {
                            id = request[i].id,
                            storeItemType = request[i].storeItemType,
                            status = StoreItemStatus.Found,
                            streamVersion = request[i].streamVersion,
                            buildplateWorldDimension = buildplate.dimension,
                            buildplateWorldOffset = buildplate.offset,
                            model = buildplate.model,
                            featuredItem = itemFromMap?.featuredItem,
                            inventoryCounts = itemFromMap?.inventoryCounts
                        });
                    } else {
                        result.Add(new StoreItemInfo() {
                            id = request[i].id,
                            storeItemType = request[i].storeItemType,
                            status = StoreItemStatus.NotFound,
                            streamVersion = request[i].streamVersion
                        });
                    }
                }
            }
            var response = new StoreItemInfoResponse() {
                result = result, continuationToken = null, expiration = null, updates = new Updates()
            };
            return response;
        }
    }
}