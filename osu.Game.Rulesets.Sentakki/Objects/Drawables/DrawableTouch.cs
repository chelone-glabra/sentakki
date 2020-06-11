using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Game.Rulesets.Sentakki.Objects.Drawables.Pieces;
using osu.Game.Rulesets.Objects.Drawables;
using osu.Game.Rulesets.Scoring;
using osuTK;
using osuTK.Graphics;
using System.Diagnostics;
using osu.Game.Rulesets.Sentakki.UI;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Transforms;
using osu.Game.Rulesets.Sentakki.Configuration;

namespace osu.Game.Rulesets.Sentakki.Objects.Drawables
{
    public class DrawableTouch : DrawableSentakkiHitObject, IDrawableHitObjectWithProxiedApproach
    {
        // IsHovered is used
        public override bool HandlePositionalInput => true;

        public Drawable ProxiedLayer => this;

        protected override float SamplePlaybackPosition => (HitObject.Position.X + SentakkiPlayfield.INTERSECTDISTANCE) / (SentakkiPlayfield.INTERSECTDISTANCE * 2);

        protected override double InitialLifetimeOffset => 6000;

        private readonly TouchBlob blob1;
        private readonly TouchBlob blob2;
        private readonly TouchBlob blob3;
        private readonly TouchBlob blob4;

        private readonly TouchFlashPiece flash;
        private readonly ExplodePiece explode;

        private readonly CircularContainer dot;

        private SentakkiInputManager sentakkiActionInputManager;
        internal SentakkiInputManager SentakkiActionInputManager => sentakkiActionInputManager ??= GetContainingInputManager() as SentakkiInputManager;

        public DrawableTouch(SentakkiHitObject hitObject) : base(hitObject)
        {
            Size = new Vector2(80);
            Position = hitObject.Position;
            Origin = Anchor.Centre;
            Anchor = Anchor.Centre;
            Alpha = 0;
            Scale = Vector2.Zero;
            Colour = HitObject.NoteColor;
            AlwaysPresent = true;
            AddRangeInternal(new Drawable[]{
                blob1 = new TouchBlob{
                    Position = new Vector2(40, 0)
                },
                blob2 = new TouchBlob{
                    Position = new Vector2(-40, 0)
                },
                blob3 = new TouchBlob{
                    Position = new Vector2(0, 40)
                },
                blob4 = new TouchBlob{
                    Position = new Vector2(0, -40)
                },
                dot = new CircularContainer
                {
                    Size = new Vector2(20),
                    Masking = true,
                    BorderColour = Color4.Gray,
                    BorderThickness = 2,
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    Child = new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        AlwaysPresent = true,
                        Colour = Color4.White,
                    }
                },
                flash = new TouchFlashPiece(),
                explode = new ExplodePiece(),
                new HitReceptor{
                    Hit = () =>
                    {
                        if (AllJudged)
                            return false;

                        UpdateResult(true);
                        return false;
                    },
                }
            });
        }

        private readonly Bindable<double> touchAnimationDuration = new Bindable<double>(1000);

        [BackgroundDependencyLoader(true)]
        private void load(SentakkiRulesetConfigManager sentakkiConfigs)
        {
            sentakkiConfigs?.BindWith(SentakkiRulesetSettings.TouchAnimationDuration, touchAnimationDuration);
        }

        // Easing functions for manual use.
        private readonly DefaultEasingFunction inOutBack = new DefaultEasingFunction(Easing.InOutBack);
        private readonly DefaultEasingFunction inQuint = new DefaultEasingFunction(Easing.InQuint);

        protected override void Update()
        {
            base.Update();
            if (Result.HasResult) return;

            double fadeIn = touchAnimationDuration.Value * GameplaySpeed;
            double moveTo = 500 * GameplaySpeed;
            double animStart = HitObject.StartTime - fadeIn - moveTo;
            double currentProg = Clock.CurrentTime - animStart;

            // Calculate initial entry animation
            float fadeAmount = (float)(currentProg / fadeIn);
            if (fadeAmount < 0) fadeAmount = 0;
            else if (fadeAmount > 1) fadeAmount = 1;

            Alpha = fadeAmount * (float)inOutBack.ApplyEasing(fadeAmount);
            Scale = new Vector2(1f * fadeAmount * (float)inOutBack.ApplyEasing(fadeAmount));

            // Calculate position
            float moveAmount = (float)((currentProg - fadeIn) / moveTo);
            if (moveAmount < 0) moveAmount = 0;
            else if (moveAmount > 1) moveAmount = 1;

            // Used to simplify this crazy arse manual animating
            float moveAnimFormula(float originalValue) => (float)(originalValue - (originalValue * moveAmount * inQuint.ApplyEasing(moveAmount)));

            blob1.Position = new Vector2(moveAnimFormula(40), 0);
            blob2.Position = new Vector2(moveAnimFormula(-40), 0);
            blob3.Position = new Vector2(0, moveAnimFormula(40));
            blob4.Position = new Vector2(0, moveAnimFormula(-40));

            // Used to simplify this crazy arse manual animating
            float sizeAnimFormula() => (float)(.5 + .5 * moveAmount * inQuint.ApplyEasing(moveAmount));

            blob1.Scale = new Vector2(sizeAnimFormula());
            blob2.Scale = new Vector2(sizeAnimFormula());
            blob3.Scale = new Vector2(sizeAnimFormula());
            blob4.Scale = new Vector2(sizeAnimFormula());

            // Handle hidden and fadeIn modifications
            if (IsHidden)
            {
                float hideAmount = (float)((currentProg - fadeIn) / (moveTo / 2));
                if (hideAmount < 0) hideAmount = 0;
                else if (hideAmount > 1) hideAmount = 1;

                Alpha = 1 - (1 * hideAmount);
            }
            else if (IsFadeIn)
            {
                // Using existing moveAmount because it serves our needs
                Alpha = 1 * moveAmount;
            }

        }

        protected override void CheckForResult(bool userTriggered, double timeOffset)
        {
            Debug.Assert(HitObject.HitWindows != null);

            if (!userTriggered)
            {
                if (Auto && timeOffset > 0)
                    ApplyResult(r => r.Type = HitResult.Perfect);

                if (!HitObject.HitWindows.CanBeHit(timeOffset))
                    ApplyResult(r => r.Type = HitResult.Miss);

                return;
            }
            var result = HitObject.HitWindows.ResultFor(timeOffset);
            if (timeOffset < 0 && result <= HitResult.Miss)
                return;
            if (result >= HitResult.Meh && timeOffset < 0)
                result = HitResult.Perfect;

            ApplyResult(r => r.Type = result);
        }

        protected override void UpdateStateTransforms(ArmedState state)
        {
            base.UpdateStateTransforms(state);
            const double time_fade_hit = 400, time_fade_miss = 400;

            switch (state)
            {
                case ArmedState.Hit:
                    const double flash_in = 40;
                    const double flash_out = 100;

                    flash.FadeTo(0.8f, flash_in)
                         .Then()
                         .FadeOut(flash_out);

                    dot.Delay(flash_in).FadeOut();

                    explode.FadeIn(flash_in);
                    this.ScaleTo(1.5f, 400, Easing.OutQuad);

                    this.Delay(time_fade_hit).FadeOut().Expire();

                    break;

                case ArmedState.Miss:
                    this.ScaleTo(0.5f, time_fade_miss, Easing.InCubic)
                       .FadeColour(Color4.Red, time_fade_miss, Easing.OutQuint)
                       .FadeOut(time_fade_miss);
                    break;
            }
        }
    }
}