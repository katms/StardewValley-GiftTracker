using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using StardewModdingAPI;

namespace SDVGiftTracker
{
    public class GiftTrackerConfig : Config
    {
        // configure which categories should be displayed
        // loves will always be shown
        public bool ShowNeutral { get; set; }
        public bool ShowLikes { get; set; }
        public bool ShowDislikes { get; set; }
        public bool ShowHates { get; set; }

        public override T GenerateDefaultConfig<T>()
        {
            ShowNeutral = false;
            ShowLikes = true;
            ShowDislikes = true;
            ShowHates = true;

            return this as T;
        }
    }
}
