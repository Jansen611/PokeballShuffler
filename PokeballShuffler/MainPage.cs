using System.Collections.ObjectModel;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Controls.Shapes;
using PokeballShuffler.Models;
using PokeballShuffler.ViewModels;

namespace PokeballShuffler;

public class MainPage : ContentPage
{
    private readonly MainViewModel _vm;

    private VerticalStackLayout[] vsl_basketContainers = new VerticalStackLayout[4];
    private VerticalStackLayout vsl_undrawn;
    private Label lbl_undrawnSubLabel;
    private Label lbl_roundLabel;
    private Button btn_shuffle;
    private ImageButton btn_reset;
    private Button btn_modeToggle;
    private Border? brd_inventoryPanel;
    private readonly List<Label> lbls_inventoryCount = new();
    private readonly List<Label> lbls_basketSub = new();

    // Character avatar images (created on demand per shuffle)
    private Image? img_char1Avatar;
    private Image? img_char2Avatar;
    private Image? img_hiddenBall;
    private Grid? grd_charSlot;        // Overlays hidden ball + char1 in one layout slot
    private readonly List<BoxView> boxs_char1Spacers = new(); // Keep char1 initially at slot 4
    private readonly List<BoxView> boxs_char2Spacers = new(); // Keep char2 initially at slot 4
    private bool _isUndrawnAnimating;

    // Theme color sets
    private readonly (Color bg, Color basketBg, Color basketStroke, Color undrawnBg,
                      Color undrawnStroke, Color accent, Color textPrimary, Color textSecondary,
                      Color shuffleBg, Color resetBg, Color resetText, Color modeBtnNormal, Color modeBtnExtended)
        _darkColors;

    private readonly (Color bg, Color basketBg, Color basketStroke, Color undrawnBg,
                      Color undrawnStroke, Color accent, Color textPrimary, Color textSecondary,
                      Color shuffleBg, Color resetBg, Color resetText, Color modeBtnNormal, Color modeBtnExtended)
        _lightColors;

    private bool _isExtendedMode;

    public MainPage()
    {
        _vm = new MainViewModel();
        BindingContext = _vm;

        // Dark theme (Extended mode)
        _darkColors = (
            bg: Color.FromArgb("#1a1a2e"),
            basketBg: Color.FromArgb("#16213e"),
            basketStroke: Color.FromArgb("#0f3460"),
            undrawnBg: Color.FromArgb("#0f3460"),
            undrawnStroke: Color.FromArgb("#e94560"),
            accent: Color.FromArgb("#e94560"),
            textPrimary: Colors.White,
            textSecondary: Color.FromArgb("#a0a0a0"),
            shuffleBg: Color.FromArgb("#e94560"),
            resetBg: Color.FromArgb("#0f3460"),
            resetText: Color.FromArgb("#a0a0a0"),
            modeBtnNormal: Color.FromArgb("#4a4a6a"),
            modeBtnExtended: Color.FromArgb("#e94560")
        );

        // Light theme (Normal mode)
        _lightColors = (
            bg: Color.FromArgb("#f6f0df"),
            basketBg: Color.FromArgb("#eee8d5"),
            basketStroke: Color.FromArgb("#d8cfb6"),
            undrawnBg: Color.FromArgb("#e7dfc9"),
            undrawnStroke: Color.FromArgb("#e94560"),
            accent: Color.FromArgb("#e94560"),
            textPrimary: Color.FromArgb("#43565c"),
            textSecondary: Color.FromArgb("#7a7f73"),
            shuffleBg: Color.FromArgb("#e94560"),
            resetBg: Color.FromArgb("#d8cfb6"),
            resetText: Color.FromArgb("#3b3a32"),
            modeBtnNormal: Color.FromArgb("#e94560"),
            modeBtnExtended: Color.FromArgb("#8a8f83")
        );

        // Subscribe to events
        _vm.BallAddedToBasket += OnBallAddedToBasket;
        _vm.Basket4Rerolled += OnBasket4Rerolled;
        _vm.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(MainViewModel.CanShuffle))
            {
                btn_shuffle.IsEnabled = _vm.CanShuffle;
            }
            else if (e.PropertyName == nameof(MainViewModel.PoolCount))
            {
                UpdateRemainingLabel(_vm.PoolCount);
            }
            else if (e.PropertyName == nameof(MainViewModel.GameMode))
            {
                _isExtendedMode = _vm.GameMode == GameMode.Extended;
                ApplyTheme(_isExtendedMode);
                UpdateModeButton();

                // Clear all UI and re-add avatars if entering extended mode
                for (int i = 0; i < 4; i++)
                    vsl_basketContainers[i].Clear();
                vsl_undrawn.Clear();
                grd_charSlot = null;
                img_char1Avatar = null;
                img_char2Avatar = null;
                img_hiddenBall = null;
                boxs_char1Spacers.Clear();
                boxs_char2Spacers.Clear();
                _isChar1Revealed = false;
                _isUndrawnAnimating = false;
                lbl_roundLabel.Text = "Round 1 of 4";
                lbl_undrawnSubLabel.Text = "15 remaining";
                btn_shuffle.IsEnabled = true;
                btn_reset.IsEnabled = false;

                if (_isExtendedMode)
                    SetupCharAvatars();
            }
            else if (e.PropertyName == nameof(MainViewModel.IsChar2SkillAvailable))
            {
                UpdateChar2AvatarState();
            }
        };

        BuildUI();
        Content = BuildContent();
        ApplyTheme(_isExtendedMode);
        UpdateModeButton();
    }

    private void UpdateModeButton()
    {
        if (btn_modeToggle == null) return;
        bool isExtended = _vm.GameMode == GameMode.Extended;
        btn_modeToggle.Text = isExtended ? "Extended" : "Normal";
        btn_modeToggle.BackgroundColor = isExtended ? _darkColors.modeBtnExtended : _lightColors.modeBtnNormal;
        btn_modeToggle.TextColor = Colors.White;
    }

    private void BuildUI()
    {
        // Create basket containers — fixed height to hold up to 4 balls (50px each + 6px spacing × 3)
        for (int i = 0; i < 4; i++)
        {
            vsl_basketContainers[i] = new VerticalStackLayout
            {
                Spacing = 6,
                Padding = new Thickness(4),
                HeightRequest = 218  // 4 × 50 (ball height) + 3 × 6 (spacing)
            };
        }

        vsl_undrawn = new VerticalStackLayout
        {
            Spacing = 4,
            Padding = new Thickness(4),
            HeightRequest = 216 // 5 × 40 (ball height) + 4 × 4 (spacing)
        };

        lbl_roundLabel = new Label
        {
            Text = "Round 1 of 4",
            FontSize = 18,
            FontAttributes = FontAttributes.Bold,
            HorizontalOptions = LayoutOptions.Center
        };

        btn_shuffle = new Button
        {
            Text = "SHUFFLE",
            FontSize = 22,
            FontAttributes = FontAttributes.Bold,
            CornerRadius = 12,
            HeightRequest = 60
        };
        btn_shuffle.Clicked += OnShuffleClicked;

        btn_reset = new ImageButton
        {
            Source = CreateResetIconSource(_darkColors.resetText),
            CornerRadius = 12,
            HeightRequest = 50,
            WidthRequest = 50,
            Padding = new Thickness(12),
            IsEnabled = false
        };
        btn_reset.Clicked += OnResetClicked;

        btn_modeToggle = new Button
        {
            Text = "普通模式",
            FontSize = 14,
            FontAttributes = FontAttributes.Bold,
            CornerRadius = 8,
            HeightRequest = 36,
            WidthRequest = 118,
            Padding = new Thickness(12, 0)
        };
        btn_modeToggle.Clicked += (s, e) => _vm.ToggleGameModeCommand.Execute(null);
    }

    private void ApplyTheme(bool isExtended)
    {
        var c = isExtended ? _darkColors : _lightColors;
        BackgroundColor = c.bg;

        // Update top inventory panel (ball distribution reference)
        if (brd_inventoryPanel != null)
        {
            brd_inventoryPanel.BackgroundColor = c.basketBg;
            brd_inventoryPanel.Stroke = c.basketStroke;
        }
        foreach (var countLabel in lbls_inventoryCount)
        {
            countLabel.TextColor = c.textPrimary;
        }
        foreach (var subLabel in lbls_basketSub)
        {
            subLabel.TextColor = c.textSecondary;
        }
        if (lbl_undrawnSubLabel != null)
        {
            lbl_undrawnSubLabel.TextColor = c.textSecondary;
        }

        // Update basket frames via the HorizontalStackLayout in row 1 of the main grid
        if (Content is AbsoluteLayout rootLayout && rootLayout.Children.FirstOrDefault() is Grid mainGrid)
        {
            var basketRow = mainGrid.Children
                .Where(v => mainGrid.GetRow(v) == 1 && v is HorizontalStackLayout)
                .Cast<HorizontalStackLayout>()
                .FirstOrDefault();

            if (basketRow != null)
            {
                foreach (var child in basketRow.Children.OfType<Border>())
                {
                    bool isUndrawn = child.Content is VerticalStackLayout vsl &&
                                     vsl.Children.FirstOrDefault() is Label lbl &&
                                     lbl.Text == "Undrawn";
                    child.BackgroundColor = isUndrawn ? c.undrawnBg : c.basketBg;
                    child.Stroke = isUndrawn ? c.undrawnStroke : c.basketStroke;
                }
            }
        }

        // Update bottom row buttons
        if (btn_shuffle != null)
        {
            btn_shuffle.BackgroundColor = c.shuffleBg;
            btn_shuffle.TextColor = Colors.White;
        }
        if (btn_reset != null)
        {
            btn_reset.BackgroundColor = c.resetBg;
            btn_reset.Source = CreateResetIconSource(c.resetText);
        }

        // Update round label
        if (lbl_roundLabel != null)
        {
            lbl_roundLabel.TextColor = c.textPrimary;
        }
    }

    private View BuildContent()
    {
        // Background
        BackgroundColor = Color.FromArgb("#1a1a2e");

        // Create basket frames
        Border[] basketFrames = new Border[4];
        string[] basketLabels = { "Basket 1", "Basket 2", "Basket 3", "Basket 4" };
        string[] basketSubLabels = { "4 balls", "3 balls", "2 balls", "1 ball" };
        int[] expectedCounts = { 4, 3, 2, 1 };

        for (int i = 0; i < 4; i++)
        {
            var label = new Label
            {
                Text = basketLabels[i],
                FontSize = 14,
                FontAttributes = FontAttributes.Bold,
                TextColor = Color.FromArgb("#e94560"),
                HorizontalOptions = LayoutOptions.Center
            };
            var subLabel = new Label
            {
                Text = basketSubLabels[i],
                FontSize = 10,
                TextColor = Color.FromArgb("#a0a0a0"),
                HorizontalOptions = LayoutOptions.Center
            };
            lbls_basketSub.Add(subLabel);

            var basketContent = new VerticalStackLayout
            {
                Spacing = 8,
                Children = { label, subLabel, vsl_basketContainers[i] }
            };

            basketFrames[i] = new Border
            {
                BackgroundColor = Color.FromArgb("#16213e"),
                Stroke = Color.FromArgb("#0f3460"),
                StrokeThickness = 1,
                StrokeShape = new RoundRectangle { CornerRadius = 12 },
                Padding = new Thickness(12),
                WidthRequest = 160,
                Content = basketContent
            };
        }

        // Undrawn area
        var undrawnLabel = new Label
        {
            Text = "Undrawn",
            FontSize = 16,
            FontAttributes = FontAttributes.Bold,
            TextColor = Color.FromArgb("#e94560"),
            HorizontalOptions = LayoutOptions.Center
        };
        var undrawnSubLabel = lbl_undrawnSubLabel = new Label
        {
            Text = "15 remaining",
            FontSize = 10,
            TextColor = Color.FromArgb("#a0a0a0"),
            HorizontalOptions = LayoutOptions.Center
        };
        var undrawnContent = new VerticalStackLayout
        {
            Spacing = 8,
            Children = { undrawnLabel, undrawnSubLabel, vsl_undrawn }
        };
        var undrawnFrame = new Border
        {
            BackgroundColor = Color.FromArgb("#0f3460"),
            Stroke = Color.FromArgb("#e94560"),
            StrokeThickness = 1,
            StrokeShape = new RoundRectangle { CornerRadius = 12 },
            Padding = new Thickness(12),
            WidthRequest = 160,
            Content = undrawnContent
        };

        // Top inventory panel: original ball distribution (static reference)
        var inventoryItems = new (string image, int count)[]
        {
            ("poke_ball.png",    5),
            ("premier_ball.png", 4),
            ("great_ball.png",   3),
            ("ultra_ball.png",   2),
            ("master_ball.png",  1),
        };

        var inventoryRow = new HorizontalStackLayout
        {
            Spacing = 20,
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center,
        };

        foreach (var (imgSrc, count) in inventoryItems)
        {
            var countLabel = new Label
            {
                Text = $"×{count}",
                FontSize = 14,
                FontAttributes = FontAttributes.Bold,
                TextColor = Colors.White,
                HorizontalOptions = LayoutOptions.Center,
            };
            lbls_inventoryCount.Add(countLabel);

            inventoryRow.Add(new VerticalStackLayout
            {
                Spacing = 4,
                HorizontalOptions = LayoutOptions.Center,
                Children =
                {
                    new Image
                    {
                        Source = imgSrc,
                        WidthRequest = 44,
                        HeightRequest = 44,
                        HorizontalOptions = LayoutOptions.Center,
                    },
                    countLabel
                }
            });
        }

        brd_inventoryPanel = new Border
        {
            BackgroundColor = Color.FromArgb("#16213e"),
            Stroke = Color.FromArgb("#0f3460"),
            StrokeThickness = 1,
            StrokeShape = new RoundRectangle { CornerRadius = 12 },
            Padding = new Thickness(20, 12),
            HorizontalOptions = LayoutOptions.Center,
            Content = inventoryRow
        };

        // Middle row: 4 baskets + undrawn area
        var topRow = new HorizontalStackLayout
        {
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center,
            Spacing = 16
        };
        foreach (var frame in basketFrames) topRow.Add(frame);
        topRow.Add(undrawnFrame);

        // Bottom row: Round label + Shuffle + Reset buttons
        var bottomRow = new HorizontalStackLayout
        {
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center,
            Spacing = 24
        };
        btn_shuffle.WidthRequest = 160;
        btn_reset.WidthRequest = 50;
        bottomRow.Add(lbl_roundLabel);
        bottomRow.Add(btn_shuffle);
        bottomRow.Add(btn_reset);

        // Main grid: inventory (Auto) + baskets (Star, centered) + buttons (Auto)
        // Using Star for the middle row ensures the visual gap above baskets == gap below baskets.
        var grid = new Grid
        {
            RowDefinitions = new RowDefinitionCollection
            {
                new RowDefinition { Height = GridLength.Auto },
                new RowDefinition { Height = new GridLength(1, GridUnitType.Star) },
                new RowDefinition { Height = GridLength.Auto }
            },
            Padding = new Thickness(16),
            RowSpacing = 16,
            HorizontalOptions = LayoutOptions.Fill,
            VerticalOptions = LayoutOptions.Fill
        };
        grid.Add(brd_inventoryPanel);
        grid.Add(topRow);
        grid.Add(bottomRow);
        Grid.SetRow(brd_inventoryPanel, 0);
        Grid.SetRow(topRow, 1);
        Grid.SetRow(bottomRow, 2);

        // Mode toggle button overlaid at top-right corner
        var modeOverlay = new StackLayout
        {
            HorizontalOptions = LayoutOptions.End,
            VerticalOptions = LayoutOptions.Start,
            Padding = new Thickness(0, 16, 16, 0),
            Children = { btn_modeToggle }
        };

        // Wrap in AbsoluteLayout so the mode button floats over the grid
        var rootLayout = new AbsoluteLayout
        {
            HorizontalOptions = LayoutOptions.Fill,
            VerticalOptions = LayoutOptions.Fill
        };
        rootLayout.Add(grid);
        rootLayout.Add(modeOverlay);
        AbsoluteLayout.SetLayoutBounds(grid, new Rect(0, 0, 1, 1));
        AbsoluteLayout.SetLayoutFlags(grid, Microsoft.Maui.Layouts.AbsoluteLayoutFlags.All);
        AbsoluteLayout.SetLayoutBounds(modeOverlay, new Rect(1, 0, AbsoluteLayout.AutoSize, AbsoluteLayout.AutoSize));
        AbsoluteLayout.SetLayoutFlags(modeOverlay, Microsoft.Maui.Layouts.AbsoluteLayoutFlags.PositionProportional);

        return rootLayout;
    }

    private static ImageSource CreateResetIconSource(Color color)
    {
        return new FontImageSource
        {
            Glyph = "\u21bb",
            Color = color,
            Size = 24
        };
    }

    private async void OnBallAddedToBasket(Pokeball ball, int round)
    {
        if (round < 0 || round >= 4) return;

        var container = vsl_basketContainers[round];

        var image = new Image
        {
            Source = ball.ImageSource,
            WidthRequest = 50,
            HeightRequest = 50,
            Opacity = 1,
            Scale = 0.3
        };

        // In extended mode, replace placeholder slots so avatars keep their intended positions.
        int insertIndex = container.Count;
        if (_isExtendedMode && round == 0 && grd_charSlot != null)
        {
            if (boxs_char1Spacers.Count > 0)
            {
                var spacer = boxs_char1Spacers[0];
                int spacerIndex = container.IndexOf(spacer);
                if (spacerIndex >= 0)
                {
                    container.RemoveAt(spacerIndex);
                    boxs_char1Spacers.RemoveAt(0);
                    insertIndex = spacerIndex;
                }
                else
                {
                    boxs_char1Spacers.RemoveAt(0);
                    insertIndex = container.IndexOf(grd_charSlot);
                }
            }
            else
            {
                insertIndex = container.IndexOf(grd_charSlot);
            }
        }
        else if (_isExtendedMode && round == 3 && img_char2Avatar != null)
        {
            if (boxs_char2Spacers.Count > 0)
            {
                var spacer = boxs_char2Spacers[0];
                int spacerIndex = container.IndexOf(spacer);
                if (spacerIndex >= 0)
                {
                    container.RemoveAt(spacerIndex);
                    boxs_char2Spacers.RemoveAt(0);
                    insertIndex = spacerIndex;
                }
                else
                {
                    boxs_char2Spacers.RemoveAt(0);
                    insertIndex = container.IndexOf(img_char2Avatar);
                }
            }
            else
            {
                insertIndex = container.IndexOf(img_char2Avatar);
            }
        }

        if (insertIndex < 0) insertIndex = container.Count;
        container.Insert(insertIndex, image);

        await Task.Yield();
        await image.ScaleTo(1, 300);
    }

    private async void OnBasket4Rerolled(Pokeball oldBall, Pokeball newBall)
    {
        var container = vsl_basketContainers[3];
        if (container.Count == 0) return;

        // The ball image is always the first child (index 0); char2 spacers/avatar come after
        var oldImage = container[0] as Image;
        if (oldImage != null)
        {
            await oldImage.FadeTo(0, 200);
            container.Remove(oldImage);
        }

        var newImage = new Image
        {
            Source = newBall.ImageSource,
            WidthRequest = 50,
            HeightRequest = 50,
            Opacity = 0,
            Scale = 0.3
        };
        // Insert at position 0 so it stays before char2 spacers/avatar
        container.Insert(0, newImage);
        await Task.Yield();
        await Task.WhenAll(
            newImage.FadeTo(1, 200),
            newImage.ScaleTo(1, 200)
        );

        // Final reveal phase: auto-show Char1 hidden ball when Char2 rerolls.
        EnsureChar1Revealed();

        // Keep Undrawn visuals in sync after Char2 swaps balls.
        await RebuildUndrawnContainerAsync();
    }

    private async void OnShuffleClicked(object? sender, EventArgs e)
    {
        btn_shuffle.IsEnabled = false;
        btn_reset.IsEnabled = false;

        bool isFinalRoundShuffle = _vm.CanShuffle && _vm.CurrentRoundDisplay == 4;
        if (isFinalRoundShuffle)
        {
            _isUndrawnAnimating = true;
            UpdateChar2AvatarState();
        }

        try
        {
            await _vm.ShuffleCommand.ExecuteAsync(null);

            // After round 0 in extended mode, update the hidden ball image source
            if (_isExtendedMode && img_hiddenBall != null && _vm.HiddenBall != null)
            {
                img_hiddenBall.Source = _vm.HiddenBall.ImageSource;
            }
            if (_isExtendedMode)
            {
                UpdateChar1AvatarState();
            }

            int nextRound = Math.Min(_vm.CurrentRoundDisplay, 4);
            lbl_roundLabel.Text = $"Round {nextRound} of 4";

            btn_shuffle.IsEnabled = _vm.CanShuffle;
            btn_reset.IsEnabled = true;

            // After all rounds done, populate undrawn area
            if (!_vm.CanShuffle)
            {
                await Task.Delay(400);
                await PopulateUndrawnAsync();
            }
        }
        finally
        {
            if (isFinalRoundShuffle)
            {
                _isUndrawnAnimating = false;
                UpdateChar2AvatarState();
            }
        }
    }

    private void SetupCharAvatars()
    {
        if (!_isExtendedMode) return;
        if (grd_charSlot != null) return; // Already set up

        // Char1: Grid overlay with hidden ball image + char1 avatar in Basket 1
        img_hiddenBall = new Image
        {
            Source = "",  // Source updated after round 0 draws the hidden ball
            WidthRequest = 50,
            HeightRequest = 50,
            Opacity = 0,
            Scale = 0.3,
            HorizontalOptions = LayoutOptions.Center
        };

        img_char1Avatar = CreateCharAvatar("char1_avatar.png");
        img_char1Avatar.HorizontalOptions = LayoutOptions.Center;

        grd_charSlot = new Grid
        {
            HeightRequest = 50,
            WidthRequest = 50,
            HorizontalOptions = LayoutOptions.Center
        };
        grd_charSlot.Add(img_hiddenBall);
        grd_charSlot.Add(img_char1Avatar);
        grd_charSlot.GestureRecognizers.Add(new TapGestureRecognizer
        {
            Command = new Command(() => ToggleChar1Reveal())
        });

        boxs_char1Spacers.Clear();
        for (int i = 0; i < 3; i++)
        {
            var spacer = new BoxView
            {
                WidthRequest = 50,
                HeightRequest = 50,
                Opacity = 0,
                HorizontalOptions = LayoutOptions.Center
            };
            boxs_char1Spacers.Add(spacer);
            vsl_basketContainers[0].Add(spacer);
        }
        vsl_basketContainers[0].Add(grd_charSlot);

        // Char2: keep 3 slots reserved so avatar starts at slot 4 in Basket 4
        boxs_char2Spacers.Clear();
        for (int i = 0; i < 3; i++)
        {
            var spacer = new BoxView
            {
                WidthRequest = 50,
                HeightRequest = 50,
                Opacity = 0,
                HorizontalOptions = LayoutOptions.Center
            };
            boxs_char2Spacers.Add(spacer);
            vsl_basketContainers[3].Add(spacer);
        }

        img_char2Avatar = CreateCharAvatar("char2_avatar.png");
        img_char2Avatar.HorizontalOptions = LayoutOptions.Center;
        img_char2Avatar.GestureRecognizers.Add(new TapGestureRecognizer
        {
            Command = new Command(() => OnChar2Clicked())
        });

        vsl_basketContainers[3].Add(img_char2Avatar);
        UpdateChar2AvatarState();
        UpdateChar1AvatarState();
    }

    private Image CreateCharAvatar(string imageSource)
    {
        var avatar = new Image
        {
            Source = imageSource,
            WidthRequest = 50,
            HeightRequest = 50,
            Opacity = 1
        };
        // Clip to a rounded-rect matching the avatar's fixed size (Rect.Zero would clip to nothing)
        avatar.Clip = new RoundRectangleGeometry(new CornerRadius(8), new Rect(0, 0, 50, 50));
        return avatar;
    }

    private bool _isChar1Revealed = false;

    private void UpdateChar1AvatarState()
    {
        bool isAvailable = _vm.HiddenBall != null;

        if (grd_charSlot != null)
            grd_charSlot.IsEnabled = isAvailable;

        if (img_char1Avatar != null && !_isChar1Revealed)
            img_char1Avatar.Opacity = isAvailable ? 1.0 : 0.3;
    }

    private void ToggleChar1Reveal()
    {
        if (img_hiddenBall == null || img_char1Avatar == null) return;
        if (_vm.HiddenBall == null) return;

        if (_isChar1Revealed)
        {
            // Hide the revealed ball, show char1 avatar (fade transition)
            img_hiddenBall.Opacity = 0;
            img_hiddenBall.Scale = 0.3;
            img_char1Avatar.Opacity = 1;
            img_char1Avatar.Scale = 1;
        }
        else
        {
            // Show the hidden ball (animate in), hide char1 avatar
            img_hiddenBall.Opacity = 1;
            img_hiddenBall.Scale = 0.3;
            _ = img_hiddenBall.ScaleTo(1, 300);
            img_char1Avatar.Opacity = 0;
            img_char1Avatar.Scale = 0.3;
        }
        _isChar1Revealed = !_isChar1Revealed;
    }

    private void EnsureChar1Revealed()
    {
        if (img_hiddenBall == null || img_char1Avatar == null) return;
        if (_vm.HiddenBall == null) return;
        if (_isChar1Revealed) return;

        img_hiddenBall.Opacity = 1;
        img_hiddenBall.Scale = 0.3;
        _ = img_hiddenBall.ScaleTo(1, 300);
        img_char1Avatar.Opacity = 0;
        img_char1Avatar.Scale = 0.3;
        _isChar1Revealed = true;
    }

    private void OnChar2Clicked()
    {
        if (_isUndrawnAnimating) return;
        if (!_vm.IsChar2SkillAvailable) return;
        _vm.Char2RerollCommand.Execute(null);
    }

    private void UpdateChar2AvatarState()
    {
        if (img_char2Avatar == null) return;
        bool canUseChar2 = _vm.IsChar2SkillAvailable && !_isUndrawnAnimating;
        img_char2Avatar.Opacity = canUseChar2 ? 1.0 : 0.3;
        img_char2Avatar.IsEnabled = canUseChar2;
    }

    private void ClearCharAvatars()
    {
        // Remove the char1 overlay Grid from Basket 1
        if (grd_charSlot != null && vsl_basketContainers[0].Contains(grd_charSlot))
            vsl_basketContainers[0].Remove(grd_charSlot);

        foreach (var spacer in boxs_char1Spacers)
        {
            if (vsl_basketContainers[0].Contains(spacer))
                vsl_basketContainers[0].Remove(spacer);
        }
        boxs_char1Spacers.Clear();

        grd_charSlot = null;
        img_char1Avatar = null;
        img_hiddenBall = null;

        // Remove char2 avatar and spacers from Basket 4
        if (img_char2Avatar != null && vsl_basketContainers[3].Contains(img_char2Avatar))
            vsl_basketContainers[3].Remove(img_char2Avatar);
        img_char2Avatar = null;

        foreach (var spacer in boxs_char2Spacers)
        {
            if (vsl_basketContainers[3].Contains(spacer))
                vsl_basketContainers[3].Remove(spacer);
        }
        boxs_char2Spacers.Clear();

        _isChar1Revealed = false;
    }

    private void UpdateRemainingLabel(int poolCount)
    {
        lbl_undrawnSubLabel.Text = $"{poolCount} remaining";
    }

    private async Task PopulateUndrawnAsync()
    {
        await Task.Delay(200);
        foreach (var ball in _vm.UndrawnBalls)
        {
            var image = new Image
            {
                Source = ball.ImageSource,
                WidthRequest = 40,
                HeightRequest = 40,
                Opacity = 0,
                Scale = 0.3
            };

            vsl_undrawn.Add(image);

            await Task.WhenAll(
                image.FadeTo(1, 200),
                image.ScaleTo(1, 200)
            );
            await Task.Delay(80);
        }
    }

    private async Task RebuildUndrawnContainerAsync()
    {
        vsl_undrawn.Clear();

        foreach (var ball in _vm.UndrawnBalls)
        {
            var image = new Image
            {
                Source = ball.ImageSource,
                WidthRequest = 40,
                HeightRequest = 40,
                Opacity = 0,
                Scale = 0.85
            };

            vsl_undrawn.Add(image);
            await Task.WhenAll(
                image.FadeTo(1, 180),
                image.ScaleTo(1, 180)
            );
            await Task.Delay(40);
        }
    }

    private void OnResetClicked(object? sender, EventArgs e)
    {
        // Clear all visual containers
        for (int i = 0; i < 4; i++)
        {
            vsl_basketContainers[i].Clear();
        }
        vsl_undrawn.Clear();

        // Clear character avatar references
        grd_charSlot = null;
        img_char1Avatar = null;
        img_char2Avatar = null;
        img_hiddenBall = null;
        boxs_char1Spacers.Clear();
        boxs_char2Spacers.Clear();
        _isChar1Revealed = false;
        _isUndrawnAnimating = false;

        // Update UI elements
        lbl_roundLabel.Text = "Round 1 of 4";
        lbl_undrawnSubLabel.Text = "15 remaining";
        btn_shuffle.IsEnabled = true;
        btn_reset.IsEnabled = false;

        _vm.ResetCommand.Execute(null);

        // Re-add avatars if in extended mode
        if (_isExtendedMode)
            SetupCharAvatars();
    }
}