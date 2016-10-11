using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Inheritance;
using StardewValley;
using StardewValley.Menus;

using System.IO;
using Newtonsoft.Json;
using System.Text.RegularExpressions;

namespace SDVGiftTracker
{
    public class SDVGiftTracker : Mod
    {
        private static GiftTasteManager GiftManager;

        private GiftTrackerConfig ModConfig { get; set; }

        private string DirectoryName => Path.Combine(PathOnDisk, "giftdata");

        // maps an NPC to a set of (name, relationship) pairs
        private Dictionary<string, Dictionary<string, string>> Relationships { get; set; }

        // dialogue regex for each npc's possible associates' loved and hated gifts
        private Dictionary<string, List<Regex>> LoveDialogues { get; set; }
        private Dictionary<string, List<Regex>> HateDialogues { get; set; }
        private string ItemRegex => "(?<item>[a-zA-Z -]+)";

        public override void Entry(params object[] objects)
        {
            PlayerEvents.InventoryChanged += OnInventoryChanged;
            PlayerEvents.LoadedGame += OnGameLoaded;

            // save learned gift tastes at the end of the day, when the game saves
            TimeEvents.OnNewDay += (object sender, EventArgsNewDay e) =>
            {
                // only save once per day (newDay will be true the second time, and on new games)
                if (!Game1.newDay && null != GiftManager)
                {
                    GiftManager.UpdateGiftData();
                }
            };

            // dialogue hook
            MenuEvents.MenuChanged += OnDialogueBox;

            GameEvents.LoadContent += OnLoadContent;

            ModConfig = new GiftTrackerConfig().InitializeConfig(BaseConfigPath);

            Command.RegisterCommand("list_gifttastes", "List all learned gift tastes").CommandFired += list_gifttastes;

            if(!Directory.Exists(DirectoryName))
            {
                Directory.CreateDirectory(DirectoryName);
            }

            Log.Out("Gift Tracker entry");
        }

        private static void OnInventoryChanged(object sender, EventArgsInventoryChanged e)
        {
            Log.Out("Player inventory changed");

            Item gift;

            // if an item was deducted (will be either removed or a negative quantity change)
            // assume it's the first item in either list, gifts only get given one at a time
            if(e.Removed.Count() > 0 ||
                (e.QuantityChanged.Count() > 0 && e.QuantityChanged[0].StackChange < 0))
            {
                // get the first item
                gift = (e.Removed.Count() > 0 ? e.Removed : e.QuantityChanged)[0].Item;
            }

            else
            {
                return;
            }
            if (Game1.activeClickableMenu is DialogueBox)
            {
                DialogueBox dbox = (DialogueBox)Game1.activeClickableMenu;
                NPC recipient = Game1.currentSpeaker;

                // check if the dialogue box's current text
                // is among the speaker's possible reactions to a gift
                // i.e. this isn't a delivery
                if (dbox != null && recipient != null &&
                    Game1.NPCGiftTastes[recipient.name].Contains(dbox.getCurrentString()))
                {
                    GiftManager.Add(recipient.name, gift);
                }
            }
        }

        private void OnLoadContent(object sender, EventArgs e)
        {
            // reverse lookup of how npcs are related
            // Pierre["my wife"] = Caroline
            Relationships = new Dictionary<string, Dictionary<string, string>>();
            var npcdata = Game1.content.Load<Dictionary<string, string>>("Data\\NPCDispositions");

            foreach (var key in npcdata.Keys)
            {
                string[] relations = npcdata[key].Split('/')[9].Split(' ');

                // happens with a couple npcs with no relationships
                // e.g. Marlon doesn't have a birthday, so the 10th element is wrong
                if (relations.Length <= 1 || relations.Length%2 != 0) continue;
                Relationships.Add(key, new Dictionary<string, string>());

                // relationships are written as a series of name-relationship pairs
                // if the relationship is an empty string then the other NPC gets referred to by name
                // otherwise it's "my [wife/sister/son/etc.]"
                // the "my" will not be capitalized if at the beginning of a sentence
                for(int i = 0, j = 1; j < relations.Length; i += 2, j = i+1)
                {
                    Relationships[key].Add(
                        (relations[j] == "''") ? relations[i] : "my " + relations[j].Replace("_", " ").Trim('\''),
                        relations[i]);
                }

                // dialogue templates
                Dictionary<GiftTaste, List<string>> Dialogues =
                    JsonConvert.DeserializeObject<Dictionary<GiftTaste, List<string>>>(File.ReadAllText(Path.Combine(PathOnDisk, "data.json")));

                LoveDialogues = new Dictionary<string, List<Regex>>();
                HateDialogues = new Dictionary<string, List<Regex>>();
                foreach (string npc in Relationships.Keys)
                {
                    LoveDialogues.Add(npc, new List<Regex>());
                    HateDialogues.Add(npc, new List<Regex>());
                    string allrelationships = String.Format("(?<character>({0}))", String.Join("|", Relationships[npc].Keys));
                    foreach (string quote in Dialogues[GiftTaste.eGiftTaste_Love])
                    {
                        LoveDialogues[npc].Add(new Regex(String.Format(quote, allrelationships, ItemRegex)));
                    }

                    foreach(string quote in Dialogues[GiftTaste.eGiftTaste_Hate])
                    {
                        HateDialogues[npc].Add(new Regex(String.Format(quote, allrelationships, ItemRegex)));
                    }
                }
            }
        }

        // called when a save is loaded
        private void OnGameLoaded(object sender, EventArgs e)
        {
            // initialize manager
            GiftManager = new GiftTasteManager(ModConfig, 
                Path.Combine(DirectoryName, Constants.SaveFolderName + ".json"));
        }

        private void OnDialogueBox(object sender, EventArgsClickableMenuChanged e)
        {
            if(e.NewMenu is DialogueBox && null != Game1.currentSpeaker 
                && Relationships.ContainsKey(Game1.currentSpeaker.name))
            {
                DialogueBox dbox = (DialogueBox)e.NewMenu;
                CheckDialogues(Game1.currentSpeaker.name, dbox.getCurrentString(), GiftTaste.eGiftTaste_Love);
                CheckDialogues(Game1.currentSpeaker.name, dbox.getCurrentString(), GiftTaste.eGiftTaste_Hate);
            }
        }

        private void CheckDialogues(string npc, string quote, GiftTaste gt)
        {
            Dictionary<string, List<Regex>> dialogues = (GiftTaste.eGiftTaste_Love == gt) ? LoveDialogues : HateDialogues;
            Match result = dialogues[npc].Select(r => r.Match(quote)).FirstOrDefault(m => Match.Empty != m);
            if(null != result)
            {
                string character = Relationships[npc][result.Groups["character"].Value],
                       item = result.Groups["item"].Value;

                // trying to get the actual Item instance from the name
                // is probably more trouble than it's worth
                // considering they're already being stored as strings
                GiftManager.Add(character, item, gt);
            }
        }

        // todo: make this an in-game thing
        private void list_gifttastes(object sender, EventArgsCommand e)
        {
            Log.Out(GiftManager.GetGiftData(e.Command.CalledArgs));
        }
    }
}
