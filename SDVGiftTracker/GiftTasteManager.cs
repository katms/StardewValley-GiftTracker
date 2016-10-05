using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using StardewValley;
using StardewModdingAPI;

namespace SDVGiftTracker
{
    class GiftTasteManager
    {
        enum GiftTaste
        {
            eGiftTaste_Love = NPC.gift_taste_love,
            eGiftTaste_Like = NPC.gift_taste_like,
            eGiftTaste_Dislike = NPC.gift_taste_dislike,
            eGiftTaste_Hate = NPC.gift_taste_hate,
            eGiftTaste_Neutral = NPC.gift_taste_neutral
        }

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
        private Dictionary<string, Dictionary<GiftTaste, HashSet<string>>> Data;

        public GiftTasteManager()
        {
            Data = new Dictionary<string, Dictionary<GiftTaste, HashSet<string>>>();

            // add a map for all NPCs with gift tastes
            // game must have loaded before this
            foreach (var c in Game1.NPCGiftTastes.Keys)
            {
                Data.Add(c, new Dictionary<GiftTaste, HashSet<string>>());
                foreach (GiftTaste e in Enum.GetValues(typeof(GiftTaste)).Cast<GiftTaste>())
                {
                    Data[c].Add(e, new HashSet<string>());
                }
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

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            foreach(var name in Data.Keys)
            {
                sb.AppendLine(name+":");
                foreach(GiftTaste gt in Data[name].Keys)
                {
                    sb.Append("\t" + GiftTasteHelper(gt) + " ");
                    sb.Append(String.Join(", ", Data[name][gt].ToArray()));
                    sb.AppendLine();
                }
            }
            return sb.ToString();
        }
    }
}
