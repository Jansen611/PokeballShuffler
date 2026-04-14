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
        };

        BuildUI();
        Content = BuildContent();
    }

    private void BuildUI()
    {
        // Create basket containers
        for (int i = 0; i < 4; i++)
        {
            _basketContainers[i] = new VerticalStackLayout
            {
                Spacing = 6,
                Padding = new Thickness(4)
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

        // Basket row
        var basketRow = new HorizontalStackLayout
        {
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center,
            Spacing = 16
        };
        foreach (var frame in basketFrames) basketRow.Add(frame);

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
            Content = undrawnContent
        };

        // Control panel
        var controlPanel = new VerticalStackLayout
        {
            VerticalOptions = LayoutOptions.Center,
            Spacing = 24,
            Padding = new Thickness(24, 0, 0, 0),
            WidthRequest = 220
        };
        controlPanel.Add(undrawnFrame);
        controlPanel.Add(_roundLabel);
        controlPanel.Add(_shuffleBtn);
        controlPanel.Add(_resetBtn);

        // Main grid
        var grid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitionCollection
            {
                new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) },
                new ColumnDefinition { Width = GridLength.Auto }
            },
            Padding = new Thickness(16)
        };
        grid.Add(basketRow);
        grid.Add(controlPanel);
        Grid.SetColumn(basketRow, 0);
        Grid.SetColumn(controlPanel, 1);

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

        // Update remaining balls label based on current round
        UpdateRemainingLabel();
    }

    private void UpdateRemainingLabel()
    {
        // Draw counts per round: 4, 3, 2, 1
        // CurrentRoundDisplay = _currentRound + 1, so completed rounds = CurrentRoundDisplay - 1
        int[] drawCounts = { 4, 3, 2, 1 };
        int completedRounds = Math.Min(_vm.CurrentRoundDisplay - 1, drawCounts.Length);
        int drawn = 0;
        for (int i = 0; i < completedRounds; i++)
            drawn += drawCounts[i];
        int remaining = 15 - drawn;
        _undrawnSubLabel.Text = $"{remaining} remaining";
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

        // Update UI elements
        _roundLabel.Text = "Round 1 of 4";
        _undrawnSubLabel.Text = "15 remaining";
        _shuffleBtn.IsEnabled = true;
        _resetBtn.IsEnabled = false;

        _vm.ResetCommand.Execute(null);
    }
}