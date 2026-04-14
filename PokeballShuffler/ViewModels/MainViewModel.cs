using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PokeballShuffler.Models;

namespace PokeballShuffler.ViewModels;

/// <summary>
/// Main ViewModel for the Pokeball Shuffler game.
/// Handles all game logic: shuffle draws, basket management, reset.
/// </summary>
public partial class MainViewModel : ObservableObject
{
    // Draw counts per round: round 1 → 4, round 2 → 3, round 3 → 2, round 4 → 1
    private static readonly int[] DrawCounts = { 4, 3, 2, 1 };

    private List<Pokeball> _pool;
    private int _currentRound;

    // Baskets — each is a collection of Pokeballs drawn in that round
    public ObservableCollection<Pokeball> Basket1 { get; } = new();
    public ObservableCollection<Pokeball> Basket2 { get; } = new();
    public ObservableCollection<Pokeball> Basket3 { get; } = new();
    public ObservableCollection<Pokeball> Basket4 { get; } = new();

    // Balls remaining after all 4 shuffles
    public ObservableCollection<Pokeball> UndrawnBalls { get; } = new();

    // Current round display (1-based for user display)
    public int CurrentRoundDisplay => _currentRound + 1;

    // Number of balls still in the pool (undrawn)
    public int PoolCount => _pool.Count;

    // Whether Shuffle button is enabled
    public bool CanShuffle => _currentRound < 4;

    // Event raised when a new ball is added to a basket — used by View for animations
    public event Action<Pokeball, int>? BallAddedToBasket;

    public MainViewModel()
    {
        _pool = CreateInitialPool();
        _currentRound = 0;
    }

    /// <summary>
    /// Creates the initial pool of 15 balls:
    /// 5 Poké Ball, 4 Premier Ball, 3 Great Ball, 2 Ultra Ball, 1 Master Ball.
    /// </summary>
    private static List<Pokeball> CreateInitialPool()
    {
        var pool = new List<Pokeball>();

        // Add balls in a fixed order — shuffle determines randomness
        for (int i = 0; i < 5; i++) pool.Add(new Pokeball(BallType.PokeBall));
        for (int i = 0; i < 4; i++) pool.Add(new Pokeball(BallType.PremierBall));
        for (int i = 0; i < 3; i++) pool.Add(new Pokeball(BallType.GreatBall));
        for (int i = 0; i < 2; i++) pool.Add(new Pokeball(BallType.UltraBall));
        pool.Add(new Pokeball(BallType.MasterBall));

        return pool;
    }

    /// <summary>
    /// Performs the next shuffle: draws balls from pool into the next basket.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanShuffle))]
    public async Task ShuffleAsync()
    {
        if (_currentRound >= 4) return;

        int drawCount = DrawCounts[_currentRound];
        ObservableCollection<Pokeball> targetBasket = _currentRound switch
        {
            0 => Basket1,
            1 => Basket2,
            2 => Basket3,
            3 => Basket4,
            _ => Basket1
        };

        // Fisher-Yates shuffle on the remaining pool to pick random balls
        var poolCopy = new List<Pokeball>(_pool);
        int remaining = poolCopy.Count;

        for (int i = 0; i < drawCount; i++)
        {
            int idx = Random.Shared.Next(poolCopy.Count);
            var ball = poolCopy[idx];

            // Remove from pool copy
            poolCopy.RemoveAt(idx);

            // Add to basket
            targetBasket.Add(ball);

            // Remove from actual pool
            _pool.Remove(ball);

            // Raise event for View to animate this ball
            BallAddedToBasket?.Invoke(ball, _currentRound);

            // Notify UI that pool count changed (updates "remaining" label immediately)
            OnPropertyChanged(nameof(PoolCount));

            // Small delay between each ball appearing (for staggered animation)
            await Task.Delay(150);
        }

        _currentRound++;
        OnPropertyChanged(nameof(CurrentRoundDisplay));
        OnPropertyChanged(nameof(CanShuffle));

        // After round 4, remaining balls go to UndrawnBalls
        if (_currentRound >= 4)
        {
            foreach (var ball in _pool)
            {
                UndrawnBalls.Add(ball);
            }
            _pool.Clear();
        }

        ShuffleCommand.NotifyCanExecuteChanged();
    }

    /// <summary>
    /// Resets the game: clears all baskets, refills the pool, resets round counter.
    /// </summary>
    [RelayCommand]
    public void Reset()
    {
        Basket1.Clear();
        Basket2.Clear();
        Basket3.Clear();
        Basket4.Clear();
        UndrawnBalls.Clear();

        _pool = CreateInitialPool();
        _currentRound = 0;

        OnPropertyChanged(nameof(CurrentRoundDisplay));
        OnPropertyChanged(nameof(CanShuffle));
        OnPropertyChanged(nameof(PoolCount));
        ShuffleCommand.NotifyCanExecuteChanged();
    }
}