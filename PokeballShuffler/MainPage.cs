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

    private VerticalStackLayout[] _basketContainers = new VerticalStackLayout[4];
    private VerticalStackLayout _undrawnContainer;
    private Label _undrawnSubLabel;
    private Label _roundLabel;
    private Button _shuffleBtn;
    private Button _resetBtn;
    private Button _modeToggleBtn;

    // Character avatar images (created on demand per shuffle)
    private Image? _char1Avatar;
    private Image? _char2Avatar;
    private Image? _hiddenBallImage;
    private Grid? _charSlot;        // Overlays hidden ball + char1 in one layout slot
    private BoxView? _char2Spacer;   // Spacer to align char2 with char1 position

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
            bg: Color.FromArgb("#f0f0f5"),
            basketBg: Color.FromArgb("#e8eaf0"),
            basketStroke: Color.FromArgb("#c0c4d0"),
            undrawnBg: Color.FromArgb("#d0d4e0"),
            undrawnStroke: Color.FromArgb("#e94560"),
            accent: Color.FromArgb("#e94560"),
            textPrimary: Color.FromArgb("#1a1a2e"),
            textSecondary: Color.FromArgb("#606070"),
            shuffleBg: Color.FromArgb("#e94560"),
            resetBg: Color.FromArgb("#c0c4d0"),
            resetText: Color.FromArgb("#1a1a2e"),
            modeBtnNormal: Color.FromArgb("#e94560"),
            modeBtnExtended: Color.FromArgb("#4a4a6a")
        );

        // Subscribe to events
        _vm.BallAddedToBasket += OnBallAddedToBasket;
        _vm.Basket4Rerolled += OnBasket4Rerolled;
        _vm.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(MainViewModel.CanShuffle))
            {
                _shuffleBtn.IsEnabled = _vm.CanShuffle;
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
                    _basketContainers[i].Clear();
                _undrawnContainer.Clear();
                _charSlot = null;
                _char1Avatar = null;
                _char2Avatar = null;
                _hiddenBallImage = null;
                _char2Spacer = null;
                _isChar1Revealed = false;
                _roundLabel.Text = "Round 1 of 4";
                _undrawnSubLabel.Text = "15 remaining";
                _shuffleBtn.IsEnabled = true;
                _resetBtn.IsEnabled = false;

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
        if (_modeToggleBtn == null) return;
        bool isExtended = _vm.GameMode == GameMode.Extended;
        _modeToggleBtn.Text = isExtended ? "Extended" : "Normal";
        _modeToggleBtn.BackgroundColor = isExtended ? _darkColors.modeBtnExtended : _lightColors.modeBtnNormal;
    }

    private void BuildUI()
    {
        // Create basket containers — fixed height to hold up to 4 balls (50px each + 6px spacing × 3)
        for (int i = 0; i < 4; i++)
        {
            _basketContainers[i] = new VerticalStackLayout
            {
                Spacing = 6,
                Padding = new Thickness(4),
                HeightRequest = 218  // 4 × 50 (ball height) + 3 × 6 (spacing)
            };
        }

        _undrawnContainer = new VerticalStackLayout
        {
            Spacing = 4,
            Padding = new Thickness(4)
        };

        _roundLabel = new Label
        {
            Text = "Round 1 of 4",
            FontSize = 18,
            FontAttributes = FontAttributes.Bold,
            HorizontalOptions = LayoutOptions.Center
        };

        _shuffleBtn = new Button
        {
            Text = "SHUFFLE",
            FontSize = 22,
            FontAttributes = FontAttributes.Bold,
            CornerRadius = 12,
            HeightRequest = 60
        };
        _shuffleBtn.Clicked += OnShuffleClicked;

        _resetBtn = new Button
        {
            Text = "RESET",
            FontSize = 18,
            FontAttributes = FontAttributes.Bold,
            CornerRadius = 12,
            HeightRequest = 50,
            IsEnabled = false
        };
        _resetBtn.Clicked += OnResetClicked;

        _modeToggleBtn = new Button
        {
            Text = "普通模式",
            FontSize = 14,
            FontAttributes = FontAttributes.Bold,
            CornerRadius = 8,
            HeightRequest = 36,
            Padding = new Thickness(12, 0)
        };
        _modeToggleBtn.Clicked += (s, e) => _vm.ToggleGameModeCommand.Execute(null);
    }

    private void ApplyTheme(bool isExtended)
    {
        var c = isExtended ? _darkColors : _lightColors;
        BackgroundColor = c.bg;

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
        if (_shuffleBtn != null)
        {
            _shuffleBtn.BackgroundColor = c.shuffleBg;
            _shuffleBtn.TextColor = Colors.White;
        }
        if (_resetBtn != null)
        {
            _resetBtn.BackgroundColor = c.resetBg;
            _resetBtn.TextColor = c.resetText;
        }

        // Update round label
        if (_roundLabel != null)
        {
            _roundLabel.TextColor = c.textPrimary;
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

            var basketContent = new VerticalStackLayout
            {
                Spacing = 8,
                Children = { label, subLabel, _basketContainers[i] }
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
        var undrawnSubLabel = _undrawnSubLabel = new Label
        {
            Text = "15 remaining",
            FontSize = 10,
            TextColor = Color.FromArgb("#a0a0a0"),
            HorizontalOptions = LayoutOptions.Center
        };
        var undrawnContent = new VerticalStackLayout
        {
            Spacing = 8,
            Children = { undrawnLabel, undrawnSubLabel, _undrawnContainer }
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
                    new Label
                    {
                        Text = $"×{count}",
                        FontSize = 14,
                        FontAttributes = FontAttributes.Bold,
                        TextColor = Colors.White,
                        HorizontalOptions = LayoutOptions.Center,
                    }
                }
            });
        }

        var inventoryPanel = new Border
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
        _shuffleBtn.WidthRequest = 160;
        _resetBtn.WidthRequest = 120;
        bottomRow.Add(_roundLabel);
        bottomRow.Add(_shuffleBtn);
        bottomRow.Add(_resetBtn);

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
        grid.Add(inventoryPanel);
        grid.Add(topRow);
        grid.Add(bottomRow);
        Grid.SetRow(inventoryPanel, 0);
        Grid.SetRow(topRow, 1);
        Grid.SetRow(bottomRow, 2);

        // Mode toggle button overlaid at top-right corner
        var modeOverlay = new StackLayout
        {
            HorizontalOptions = LayoutOptions.End,
            VerticalOptions = LayoutOptions.Start,
            Padding = new Thickness(0, 16, 16, 0),
            Children = { _modeToggleBtn }
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

    private async void OnBallAddedToBasket(Pokeball ball, int round)
    {
        if (round < 0 || round >= 4) return;

        var container = _basketContainers[round];

        var image = new Image
        {
            Source = ball.ImageSource,
            WidthRequest = 50,
            HeightRequest = 50,
            Opacity = 1,
            Scale = 0.3
        };

        // In extended mode, insert balls before the avatar elements
        int insertIndex = container.Count;
        if (_isExtendedMode && round == 0 && _charSlot != null)
            insertIndex = container.IndexOf(_charSlot);
        else if (_isExtendedMode && round == 3 && _char2Avatar != null)
            insertIndex = _char2Spacer != null ? container.IndexOf(_char2Spacer) : container.IndexOf(_char2Avatar);

        if (insertIndex < 0) insertIndex = container.Count;
        container.Insert(insertIndex, image);

        await Task.Yield();
        await image.ScaleTo(1, 300);
    }

    private async void OnBasket4Rerolled(Pokeball oldBall, Pokeball newBall)
    {
        var container = _basketContainers[3];
        if (container.Count == 0) return;

        // The ball image is always the first child (index 0); char2 spacer/avatar come after
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
        // Insert at position 0 so it stays before char2 spacer/avatar
        container.Insert(0, newImage);
        await Task.Yield();
        await Task.WhenAll(
            newImage.FadeTo(1, 200),
            newImage.ScaleTo(1, 200)
        );
    }

    private async void OnShuffleClicked(object? sender, EventArgs e)
    {
        _shuffleBtn.IsEnabled = false;
        _resetBtn.IsEnabled = false;

        await _vm.ShuffleCommand.ExecuteAsync(null);

        // After round 0 in extended mode, update the hidden ball image source
        if (_isExtendedMode && _hiddenBallImage != null && _vm.HiddenBall != null)
        {
            _hiddenBallImage.Source = _vm.HiddenBall.ImageSource;
        }

        int nextRound = Math.Min(_vm.CurrentRoundDisplay, 4);
        _roundLabel.Text = $"Round {nextRound} of 4";

        _shuffleBtn.IsEnabled = _vm.CanShuffle;
        _resetBtn.IsEnabled = true;

        // After all rounds done, populate undrawn area
        if (!_vm.CanShuffle)
        {
            await Task.Delay(400);
            await PopulateUndrawnAsync();
        }
    }

    private void SetupCharAvatars()
    {
        if (!_isExtendedMode) return;
        if (_charSlot != null) return; // Already set up

        // Char1: Grid overlay with hidden ball image + char1 avatar in Basket 1
        _hiddenBallImage = new Image
        {
            Source = "",  // Source updated after round 0 draws the hidden ball
            WidthRequest = 50,
            HeightRequest = 50,
            Opacity = 0,
            Scale = 0.3,
            HorizontalOptions = LayoutOptions.Center
        };

        _char1Avatar = CreateCharAvatar("char1_avatar.png");
        _char1Avatar.HorizontalOptions = LayoutOptions.Center;

        _charSlot = new Grid
        {
            HeightRequest = 50,
            WidthRequest = 50,
            HorizontalOptions = LayoutOptions.Center
        };
        _charSlot.Add(_hiddenBallImage);
        _charSlot.Add(_char1Avatar);
        _charSlot.GestureRecognizers.Add(new TapGestureRecognizer
        {
            Command = new Command(() => ToggleChar1Reveal())
        });

        _basketContainers[0].Add(_charSlot);

        // Char2: avatar in Basket 4
        _char2Avatar = CreateCharAvatar("char2_avatar.png");
        _char2Avatar.HorizontalOptions = LayoutOptions.Center;
        _char2Avatar.GestureRecognizers.Add(new TapGestureRecognizer
        {
            Command = new Command(() => OnChar2Clicked())
        });

        _basketContainers[3].Add(_char2Avatar);
        UpdateChar2AvatarState();
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

    private void ToggleChar1Reveal()
    {
        if (_hiddenBallImage == null || _char1Avatar == null) return;

        if (_isChar1Revealed)
        {
            // Hide the revealed ball, show char1 avatar (fade transition)
            _hiddenBallImage.Opacity = 0;
            _hiddenBallImage.Scale = 0.3;
            _char1Avatar.Opacity = 1;
            _char1Avatar.Scale = 1;
        }
        else
        {
            // Show the hidden ball (animate in), hide char1 avatar
            _hiddenBallImage.Opacity = 1;
            _hiddenBallImage.Scale = 0.3;
            _ = _hiddenBallImage.ScaleTo(1, 300);
            _char1Avatar.Opacity = 0;
            _char1Avatar.Scale = 0.3;
        }
        _isChar1Revealed = !_isChar1Revealed;
    }

    private void OnChar2Clicked()
    {
        if (!_vm.IsChar2SkillAvailable) return;
        _vm.Char2RerollCommand.Execute(null);
    }

    private void UpdateChar2AvatarState()
    {
        if (_char2Avatar == null) return;
        _char2Avatar.Opacity = _vm.IsChar2SkillAvailable ? 1.0 : 0.3;
    }

    private void ClearCharAvatars()
    {
        // Remove the char1 overlay Grid from Basket 1
        if (_charSlot != null && _basketContainers[0].Contains(_charSlot))
            _basketContainers[0].Remove(_charSlot);
        _charSlot = null;
        _char1Avatar = null;
        _hiddenBallImage = null;

        // Remove char2 avatar and spacer from Basket 4
        if (_char2Avatar != null && _basketContainers[3].Contains(_char2Avatar))
            _basketContainers[3].Remove(_char2Avatar);
        _char2Avatar = null;

        if (_char2Spacer != null && _basketContainers[3].Contains(_char2Spacer))
            _basketContainers[3].Remove(_char2Spacer);
        _char2Spacer = null;

        _isChar1Revealed = false;
    }

    private void UpdateRemainingLabel(int poolCount)
    {
        _undrawnSubLabel.Text = $"{poolCount} remaining";
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

            _undrawnContainer.Add(image);

            await Task.WhenAll(
                image.FadeTo(1, 200),
                image.ScaleTo(1, 200)
            );
            await Task.Delay(80);
        }
    }

    private void OnResetClicked(object? sender, EventArgs e)
    {
        // Clear all visual containers
        for (int i = 0; i < 4; i++)
        {
            _basketContainers[i].Clear();
        }
        _undrawnContainer.Clear();

        // Clear character avatar references
        _charSlot = null;
        _char1Avatar = null;
        _char2Avatar = null;
        _hiddenBallImage = null;
        _char2Spacer = null;
        _isChar1Revealed = false;

        // Update UI elements
        _roundLabel.Text = "Round 1 of 4";
        _undrawnSubLabel.Text = "15 remaining";
        _shuffleBtn.IsEnabled = true;
        _resetBtn.IsEnabled = false;

        _vm.ResetCommand.Execute(null);

        // Re-add avatars if in extended mode
        if (_isExtendedMode)
            SetupCharAvatars();
    }
}