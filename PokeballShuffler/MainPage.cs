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

    public MainPage()
    {
        _vm = new MainViewModel();
        BindingContext = _vm;

        // Subscribe to ball addition events for animation triggering
        _vm.BallAddedToBasket += OnBallAddedToBasket;
        _vm.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(MainViewModel.CanShuffle))
            {
                _shuffleBtn.IsEnabled = _vm.CanShuffle;
            }
            else if (e.PropertyName == nameof(MainViewModel.PoolCount))
            {
                // Update label immediately after each ball is drawn (before animation completes)
                UpdateRemainingLabel(_vm.PoolCount);
            }
        };

        BuildUI();
        Content = BuildContent();
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
            TextColor = Colors.White,
            HorizontalOptions = LayoutOptions.Center
        };

        _shuffleBtn = new Button
        {
            Text = "SHUFFLE",
            FontSize = 22,
            FontAttributes = FontAttributes.Bold,
            BackgroundColor = Color.FromArgb("#e94560"),
            TextColor = Colors.White,
            CornerRadius = 12,
            HeightRequest = 60
        };
        _shuffleBtn.Clicked += OnShuffleClicked;

        _resetBtn = new Button
        {
            Text = "RESET",
            FontSize = 18,
            FontAttributes = FontAttributes.Bold,
            BackgroundColor = Color.FromArgb("#0f3460"),
            TextColor = Color.FromArgb("#a0a0a0"),
            CornerRadius = 12,
            HeightRequest = 50,
            IsEnabled = false
        };
        _resetBtn.Clicked += OnResetClicked;
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
            RowSpacing = 16
        };
        grid.Add(inventoryPanel);
        grid.Add(topRow);
        grid.Add(bottomRow);
        Grid.SetRow(inventoryPanel, 0);
        Grid.SetRow(topRow, 1);
        Grid.SetRow(bottomRow, 2);

        return grid;
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
            Opacity = 1,   // Start visible — FadeTo can fail on newly-added views
            Scale = 0.3
        };

        container.Add(image);

        // Yield to let MAUI attach the handler before animating
        await Task.Yield();
        await image.ScaleTo(1, 300);
    }

    private async void OnShuffleClicked(object? sender, EventArgs e)
    {
        _shuffleBtn.IsEnabled = false;
        _resetBtn.IsEnabled = false;

        await _vm.ShuffleCommand.ExecuteAsync(null);

        // CurrentRoundDisplay = completed rounds + 1; cap display at 4 when all done
        int nextRound = Math.Min(_vm.CurrentRoundDisplay, 4);
        _roundLabel.Text = $"Round {nextRound} of 4";

        _shuffleBtn.IsEnabled = _vm.CanShuffle;
        _resetBtn.IsEnabled = true;

        if (!_vm.CanShuffle)  // All 4 rounds done (CanShuffle = false when _currentRound >= 4)
        {
            _shuffleBtn.IsEnabled = false;
            await Task.Delay(400); // Wait for Basket4 animation to fully complete
            await PopulateUndrawnAsync();
        }
    }

    private void UpdateRemainingLabel(int poolCount)
    {
        _undrawnSubLabel.Text = $"{poolCount} remaining";
    }

    private async Task PopulateUndrawnAsync()
    {
        // Label is already correct (set by PoolCount PropertyChanged handler during draw) — do not touch it here
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

        // Update UI elements
        _roundLabel.Text = "Round 1 of 4";
        _undrawnSubLabel.Text = "15 remaining";
        _shuffleBtn.IsEnabled = true;
        _resetBtn.IsEnabled = false;

        _vm.ResetCommand.Execute(null);
    }
}