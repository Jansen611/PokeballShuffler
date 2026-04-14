using PokeballShuffler.Models;
using PokeballShuffler.ViewModels;

namespace PokeballShuffler;

public partial class MainPage : ContentPage
{
    private readonly MainViewModel _vm;

    public MainPage()
    {
        InitializeComponent();
        _vm = new MainViewModel();
        BindingContext = _vm;

        // Subscribe to ball addition events for animation triggering
        _vm.BallAddedToBasket += OnBallAddedToBasket;
        _vm.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(MainViewModel.CanShuffle))
            {
                ShuffleBtn.IsEnabled = _vm.CanShuffle;
            }
        };
    }

    private async void OnBallAddedToBasket(Pokeball ball, int round)
    {
        // Get the correct container for this round (0-based)
        var container = round switch
        {
            0 => Basket1Container,
            1 => Basket2Container,
            2 => Basket3Container,
            3 => Basket4Container,
            _ => null
        };

        if (container == null) return;

        // Create Image control for the ball
        var image = new Image
        {
            Source = ball.ImageSource,
            WidthRequest = 50,
            HeightRequest = 50,
            Opacity = 0,
            Scale = 0.3
        };

        container.Add(image);

        // Animate: fade in + scale up with easing
        await Task.WhenAll(
            image.FadeTo(1, 300),
            image.ScaleTo(1, 300)
        );
    }

    private async void OnShuffleClicked(object? sender, EventArgs e)
    {
        // Disable button during animation
        ShuffleBtn.IsEnabled = false;
        ResetBtn.IsEnabled = false;

        await _vm.ShuffleCommand.ExecuteAsync(null);

        // Update round label
        RoundLabel.Text = $"Round {_vm.CurrentRoundDisplay} of 4";

        // Update button states after shuffle completes
        ShuffleBtn.IsEnabled = _vm.CanShuffle;
        ResetBtn.IsEnabled = true;

        // After round 4, populate undrawn area
        if (_vm.CurrentRoundDisplay >= 4)
        {
            ShuffleBtn.IsEnabled = false;
            await PopulateUndrawnAsync();
        }
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

            UndrawnContainer.Add(image);

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
        Basket1Container.Clear();
        Basket2Container.Clear();
        Basket3Container.Clear();
        Basket4Container.Clear();
        UndrawnContainer.Clear();

        // Update UI elements
        RoundLabel.Text = "Round 1 of 4";
        ShuffleBtn.IsEnabled = true;
        ResetBtn.IsEnabled = false;

        _vm.ResetCommand.Execute(null);
    }
}
