using System.Collections.Generic;
using osu.Framework.Graphics;
using osuTK;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.Sentakki.UI;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.UI;
using osu.Game.Rulesets.UI.Scrolling;

namespace osu.Game.Rulesets.Sentakki.Edit
{
    public class DrawableSentakkiEditRuleset : DrawableSentakkiRuleset
    {
        public DrawableSentakkiEditRuleset(Ruleset ruleset, IBeatmap beatmap, IReadOnlyList<Mod> mods)
            : base(ruleset, beatmap, mods)
        {
        }
        // public new IScrollingInfo ScrollingInfo => base.ScrollingInfo;
        protected override Playfield CreatePlayfield() => new SentakkiEditPlayfield();

        private class SentakkiEditPlayfield : SentakkiPlayfield
        {
            protected override GameplayCursorContainer CreateCursor() => null;
        }
    }


}
