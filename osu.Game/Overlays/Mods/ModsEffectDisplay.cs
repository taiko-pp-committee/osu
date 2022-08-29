// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Localisation;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;
using osu.Game.Rulesets.Mods;
using osuTK;

namespace osu.Game.Overlays.Mods
{
    /// <summary>
    /// Base class for displays of mods effects.
    /// </summary>
    public abstract class ModsEffectDisplay<TValue> : Container, IHasCurrentValue<TValue>
    {
        public const float HEIGHT = 42;
        private const float transition_duration = 200;

        private readonly Box contentBackground;
        private readonly Box labelBackground;
        private readonly Container content;

        public Bindable<TValue> Current
        {
            get => current.Current;
            set => current.Current = value;
        }

        private readonly BindableWithCurrent<TValue> current = new BindableWithCurrent<TValue>();

        [Resolved]
        private OsuColour colours { get; set; } = null!;

        [Resolved]
        private OverlayColourProvider colourProvider { get; set; } = null!;

        /// <summary>
        /// Text to display in the left area of the display.
        /// </summary>
        protected abstract LocalisableString Label { get; }

        /// <summary>
        /// Calculates, does current value increase difficulty or decrease it.
        /// </summary>
        /// <param name="value">Value to calculate for. Will arrive from <see cref="Current"/> bindable once it changes.</param>
        /// <returns>Effect of the value.</returns>
        protected abstract ModEffect CalculateEffect(TValue value);

        protected virtual float ValueAreaWidth => 56;

        protected override Container<Drawable> Content => content;

        protected ModsEffectDisplay()
        {
            Height = HEIGHT;
            AutoSizeAxes = Axes.X;

            InternalChild = new InputBlockingContainer
            {
                RelativeSizeAxes = Axes.Y,
                AutoSizeAxes = Axes.X,
                Masking = true,
                CornerRadius = ModSelectPanel.CORNER_RADIUS,
                Shear = new Vector2(ShearedOverlayContainer.SHEAR, 0),
                Children = new Drawable[]
                {
                    contentBackground = new Box
                    {
                        Anchor = Anchor.CentreRight,
                        Origin = Anchor.CentreRight,
                        RelativeSizeAxes = Axes.Y,
                        Width = ValueAreaWidth + ModSelectPanel.CORNER_RADIUS
                    },
                    new GridContainer
                    {
                        RelativeSizeAxes = Axes.Y,
                        AutoSizeAxes = Axes.X,
                        ColumnDimensions = new[]
                        {
                            new Dimension(GridSizeMode.AutoSize),
                            new Dimension(GridSizeMode.Absolute, ValueAreaWidth)
                        },
                        Content = new[]
                        {
                            new Drawable[]
                            {
                                new Container
                                {
                                    RelativeSizeAxes = Axes.Y,
                                    AutoSizeAxes = Axes.X,
                                    Masking = true,
                                    CornerRadius = ModSelectPanel.CORNER_RADIUS,
                                    Children = new Drawable[]
                                    {
                                        labelBackground = new Box
                                        {
                                            RelativeSizeAxes = Axes.Both
                                        },
                                        new OsuSpriteText
                                        {
                                            Anchor = Anchor.Centre,
                                            Origin = Anchor.Centre,
                                            Margin = new MarginPadding { Horizontal = 18 },
                                            Shear = new Vector2(-ShearedOverlayContainer.SHEAR, 0),
                                            Text = Label,
                                            Font = OsuFont.Default.With(size: 17, weight: FontWeight.SemiBold)
                                        }
                                    }
                                },
                                content = new Container
                                {
                                    AutoSizeAxes = Axes.Both,
                                    Anchor = Anchor.Centre,
                                    Origin = Anchor.Centre,
                                    Shear = new Vector2(-ShearedOverlayContainer.SHEAR, 0)
                                }
                            }
                        }
                    }
                }
            };
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            labelBackground.Colour = colourProvider.Background4;
        }

        protected override void LoadComplete()
        {
            Current.BindValueChanged(e =>
            {
                var effect = CalculateEffect(e.NewValue);
                setColours(effect);
            }, true);
        }

        /// <summary>
        /// Fades colours of text and its background according to displayed value.
        /// </summary>
        /// <param name="effect">Effect of the value.</param>
        private void setColours(ModEffect effect)
        {
            switch (effect)
            {
                case ModEffect.NotChanged:
                    contentBackground.FadeColour(colourProvider.Background3, transition_duration, Easing.OutQuint);
                    content.FadeColour(Colour4.White, transition_duration, Easing.OutQuint);
                    break;

                case ModEffect.DifficultyReduction:
                    contentBackground.FadeColour(colours.ForModType(ModType.DifficultyReduction), transition_duration, Easing.OutQuint);
                    content.FadeColour(colourProvider.Background5, transition_duration, Easing.OutQuint);
                    break;

                case ModEffect.DifficultyIncrease:
                    contentBackground.FadeColour(colours.ForModType(ModType.DifficultyIncrease), transition_duration, Easing.OutQuint);
                    content.FadeColour(colourProvider.Background5, transition_duration, Easing.OutQuint);
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(effect));
            }
        }

        #region Quick implementations for CalculateEffect

        /// <summary>
        /// Converts signed integer into <see cref="ModEffect"/>. Negative values are counted as difficulty reduction, positive as increase.
        /// </summary>
        /// <param name="comparison">Value to convert. Can be obtained from CompareTo.</param>
        /// <returns>Effect.</returns>
        protected ModEffect CalculateForSign(int comparison)
        {
            if (comparison == 0)
                return ModEffect.NotChanged;
            if (comparison < 0)
                return ModEffect.DifficultyReduction;

            return ModEffect.DifficultyIncrease;
        }

        #endregion

        protected enum ModEffect
        {
            NotChanged,
            DifficultyReduction,
            DifficultyIncrease
        }
    }
}
