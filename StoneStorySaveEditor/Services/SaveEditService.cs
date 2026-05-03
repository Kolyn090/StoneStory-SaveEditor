using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization.Metadata;

namespace StoneStorySaveEditor.Services
{
    public static class SaveEditService
    {
        public static string UnlockAllCosmetics(string jsonText)
        {
            if (string.IsNullOrWhiteSpace(jsonText))
            {
                throw new ArgumentException("JSON text is empty.");
            }

            JsonNode? rootNode = JsonNode.Parse(jsonText);

            if (rootNode is not JsonObject root)
            {
                throw new Exception("JSON root is not an object.");
            }

            if (root["inventory_data"] is not JsonObject inventory)
            {
                throw new Exception("Could not find inventory_data.");
            }

            List<string> gears = BuildCosmeticGearList();

            JsonObject cosmetics = new JsonObject
            {
                ["golden"] = ToJsonArray(gears),
                ["prismatic"] = ToJsonArray(gears),
                ["glitch"] = ToJsonArray(gears),
                ["extra"] = BuildExtraColors(gears.Count)
            };

            root["cosmetics"] = cosmetics;

            string result = root.ToJsonString(new JsonSerializerOptions
            {
                WriteIndented = true,
                TypeInfoResolver = new DefaultJsonTypeInfoResolver()
            });

            return result;
        }

        private static List<string> BuildCosmeticGearList()
        {
            string[] gears1 =
            {
                "socketed_staff",
                "socketed_crossbow",
                "socketed_shield",
                "socketed_hammer",
                "socketed_sword",
                "socketed_long_sword",
                "wand",
            };

            string[] elements =
            {
                "AEther",
                "Ice",
                "Vigor",
                "Poison",
                "Fire"
            };

            string[] gears2 =
            {
                "hammer",
                "heavy_hammer",
                "skeleton_arm",
                "tower_shield",
                "lollipop_wand",
                "sword",
                "crossbow",
                "shield",
                "dashing_shield",
                "bashing_shield",
                "bardiche",
                "cult_mask",
                "blade_of_god",
                "repeating_crossbow",
                "heavy_crossbow",
                "compound_shield",
                "quarterstaff",
                "aether_talisman:AEther",
                "fire_talisman:Fire",
            };

            List<string> gears = new List<string>();

            foreach (string gear in gears1)
            {
                foreach (string element in elements)
                {
                    gears.Add(gear + ":" + element);
                }
            }

            gears.AddRange(gears1);
            gears.AddRange(gears2);

            return gears;
        }

        private static JsonArray BuildExtraColors(int count)
        {
            JsonArray array = new JsonArray();

            for (int i = 0; i < count; i++)
            {
                array.Add(new JsonObject
                {
                    ["c"] = "#000000"
                });
            }

            return array;
        }

        private static JsonArray ToJsonArray(IEnumerable<string> values)
        {
            JsonArray array = new JsonArray();

            foreach (string value in values)
            {
                array.Add(value);
            }

            return array;
        }

        private static JsonObject GetOrCreateObject(JsonObject obj, string key)
        {
            if (obj[key] is JsonObject existing)
            {
                return existing;
            }

            JsonObject created = new JsonObject();
            obj[key] = created;
            return created;
        }
    }
}