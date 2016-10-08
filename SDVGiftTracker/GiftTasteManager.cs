using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using StardewValley;
using StardewModdingAPI;

using System.IO;
using Newtonsoft.Json;

namespace SDVGiftTracker
{
    public enum GiftTaste
    {
        eGiftTaste_Love = NPC.gift_taste_love,
        eGiftTaste_Like = NPC.gift_taste_like,
        eGiftTaste_Dislike = NPC.gift_taste_dislike,
        eGiftTaste_Hate = NPC.gift_taste_hate,
        eGiftTaste_Neutral = NPC.gift_taste_neutral
    }

    class GiftTasteManager
    {
        static string GiftTasteHelper(GiftTaste gt)
        {
            switch (gt)
            {
                case GiftTaste.eGiftTaste_Love:
                    return "loves";
                case GiftTaste.eGiftTaste_Like:
                    return "likes";
                case GiftTaste.eGiftTaste_Dislike:
                    return "dislikes";
                case GiftTaste.eGiftTaste_Hate:
                    return "hates";
                case GiftTaste.eGiftTaste_Neutral:
                    return "neutral";
                default:
                    return "unknown";
            }
        }

        // maps NPCs by name to a dict of known gift tastes,
        // where the key is a taste and the value is a set of item names
        private Dictionary<string, Dictionary<GiftTaste, HashSet<string>>> Data { get; set; }
        private string GiftDataFilepath { get; set; }

        private GiftTrackerConfig ModConfig { get; set; }

        public GiftTasteManager(GiftTrackerConfig ModConfig, string GiftDataPath)
        {
            this.ModConfig = ModConfig;
            GiftDataFilepath = GiftDataPath;

            // fill Data
            if (!File.Exists(GiftDataFilepath))
            {
                try
                {
                    File.Create(GiftDataFilepath);
                }
                catch(Exception ex)
                {
                    Log.Error("Gift tracker: could not create save file");
                    Log.Error(ex.Message);
                }
                GenerateGiftTasteDictionary();
            }
            else
            {
                try
                {
                    Data = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<GiftTaste, HashSet<string>>>>(File.ReadAllText(GiftDataFilepath));
                }
                catch (Exception ex)
                {
                    Log.Error("Gift Tracker: Error loading gift data");
                    Log.Error(ex.Message);
                }

                // could happen if the file was created for a save
                // but not written to at the end of the day
                if (Data == null || Data.Count == 0)
                {
                    GenerateGiftTasteDictionary();
                }
            }
        }

        // creates a list of categories for every npc
        private void GenerateGiftTasteDictionary()
        {
            Data = new Dictionary<string, Dictionary<GiftTaste, HashSet<string>>>();

            if(null == Game1.NPCGiftTastes)
            {
                Log.Error("Gift Tracker: NPCGiftTastes not loaded");
                return;
            }

            foreach (var c in Game1.NPCGiftTastes.Keys)
            {
                // don't store data for universal loves, etc.
                if (c.StartsWith("Universal_") || Data.ContainsKey(c)) continue;
                Data.Add(c, new Dictionary<GiftTaste, HashSet<string>>());
                foreach (GiftTaste e in Enum.GetValues(typeof(GiftTaste)).Cast<GiftTaste>())
                {
                    if (!Data[c].ContainsKey(e))
                    {
                        Data[c].Add(e, new HashSet<string>());
                    }
                }
            }
        }

        public void UpdateGiftData()
        {
            try
            {
                File.WriteAllText(GiftDataFilepath, JsonConvert.SerializeObject(Data));
            }
            catch (Exception ex)
            {
                Log.Error("Gift Tracker: Error saving gift taste data");
                Log.Error(ex.Message);
            }
        }

        public void Add(string name, Item it)
        {
            NPC character = Game1.getCharacterFromName(name);
            if (null == character) return;

            // look up npc's gift taste for this item
            // reverse-engineering Game1.NPCGiftTastes is unnecessary and I will not be attempting it
            GiftTaste gt = (GiftTaste)character.getGiftTasteForThisItem(it);

            // if NPC is not listed or this gift is already known
            if(!Data.ContainsKey(name) || Data[name][gt].Contains(it.Name))
            {
                return;
            }

            Data[name][gt].Add(it.Name);

            Log.Out(name + " " + GiftTasteHelper(gt) + " " + it.Name);
        }

        public bool HasKnownGiftTastes(string name)
        {
            return Data.ContainsKey(name) &&
                // check if any non-empty categories would be displayed
                Data[name].Where(c => DisplayCategory(c.Key)).Any(e => e.Value.Count() > 0);
        }

        bool DisplayCategory(GiftTaste category)
        {
            switch(category)
            {
                case GiftTaste.eGiftTaste_Love:
                    return true;
                case GiftTaste.eGiftTaste_Like:
                    return ModConfig.ShowLikes;
                case GiftTaste.eGiftTaste_Dislike:
                    return ModConfig.ShowDislikes;
                case GiftTaste.eGiftTaste_Hate:
                    return ModConfig.ShowHates;
                case GiftTaste.eGiftTaste_Neutral:
                    return ModConfig.ShowNeutral;
                default:
                    return false;
            }
        }

        public string GetGiftData(string[] args = null)
        {
            // if no names given, output everyone
            HashSet<string> names = (args != null && args.Length > 0) ? 
                                    new HashSet<string>(args) : new HashSet<string>(Data.Keys);

            StringBuilder sb = new StringBuilder("\n");
            foreach (var name in Data.Keys)
            {
                // don't list people who weren't requested or with no data
                if (!names.Contains(name, StringComparer.OrdinalIgnoreCase) || !HasKnownGiftTastes(name)) continue;
                sb.AppendLine(name);
                foreach(GiftTaste gt in Data[name].Keys)
                {
                    // skip empty categories
                    // skip disabled categories
                    if (Data[name][gt].Count() > 0 && DisplayCategory(gt))
                    {
                        sb.Append("\t" + GiftTasteHelper(gt) + ": ");
                        sb.Append(String.Join(", ", Data[name][gt].ToArray()));
                        sb.AppendLine();
                    }
                }

            }
            return sb.ToString();
        }
    }
}
