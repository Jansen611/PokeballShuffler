using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PokeballShuffler.Models;

namespace PokeballShuffler.ViewModels;

/// <summary>
/// Main ViewModel for the Pokeball Shuffler game.
/// Handles all game logic: shuffle draws, basket management, reset, mode switching, character skills.
/// </summary>
public partial class MainViewModel : ObservableObject
{
    // Draw counts per round: round 1 → 4, round 2 → 3, round 3 → 2, round 4 → 1
    private static readonly int[] DrawCounts = { 4, 3, 2, 1 };

    private List<Pokeball> _pool;
    private int _currentRound;
    private Pokeball? _hiddenBall;

    // Baskets — each is a collection of Pokeballs drawn in that round
    public ObservableCollection<Pokeball> Basket1 { get; } = new();
    public ObservableCollection<Pokeball> Basket2 { get; } = new();
    public ObservableCollection<Pokeball> Basket3 { get; } = new();
    public ObservableCollection<Pokeball> Basket4 { get; } = new();

    // Balls remaining after all 4 shuffles
    public ObservableCollection<Pokeball> UndrawnBalls { get; } = new();

    // Game mode: Normal (standard rules) or Extended (character skills enabled)
    [ObservableProperty]
    private GameMode _gameMode = GameMode.Normal;

    // The 4th ball hidden in Basket 1 during extended mode (only visible via Char1 skill)
    public Pokeball? HiddenBall => _hiddenBall;

    // Char2 skill: available until used once per game (enabled only after all 4 rounds complete)
    [ObservableProperty]
    private bool _isChar2SkillAvailable = false;

    // Current round display (1-based for user display)
    public int CurrentRoundDisplay => _currentRound + 1;

    // Number of balls still in the pool (undrawn)
    public int PoolCount => _pool.Count;

    // Whether Shuffle button is enabled
    public bool CanShuffle => _currentRound < 4;

    // Event raised when a new ball is added to a basket — used by View for animations
    public event Action<Pokeball, int>? BallAddedToBasket;

    // Event raised when Char2 rerolls Basket 4: (oldBall, newBall)
    public event Action<Pokeball, Pokeball>? Basket4Rerolled;

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
        for (int i = 0; i < 5; i++) pool.Add(new Pokeball(BallType.PokeBall));
        for (int i = 0; i < 4; i++) pool.Add(new Pokeball(BallType.PremierBall));
        for (int i = 0; i < 3; i++) pool.Add(new Pokeball(BallType.GreatBall));
        for (int i = 0; i < 2; i++) pool.Add(new Pokeball(BallType.UltraBall));
        pool.Add(new Pokeball(BallType.MasterBall));
        return pool;
    }

    /// <summary>
    /// Toggles between Normal and Extended game mode, resetting the game in progress.
    /// </summary>
    [RelayCommand]
    public void ToggleGameMode()
    {
        GameMode = GameMode == GameMode.Normal ? GameMode.Extended : GameMode.Normal;
        Reset();
    }

    /// <summary>
    /// Char2 skill: rerolls Basket 4 by swapping it with a random ball from UndrawnBalls.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanChar2Reroll))]
    public void Char2Reroll()
    {
        if (_currentRound < 4 || !IsChar2SkillAvailable || UndrawnBalls.Count == 0)
            return;

        // Pick a random ball from UndrawnBalls
        int idx = Random.Shared.Next(UndrawnBalls.Count);
        var newBall = UndrawnBalls[idx];
        UndrawnBalls.RemoveAt(idx);

        // Swap: old Basket4 ball goes to UndrawnBalls, new ball replaces it
        var oldBall = Basket4[0];
        Basket4[0] = newBall;
        UndrawnBalls.Add(oldBall);

        IsChar2SkillAvailable = false;

        // Raise event for View to animate the swap
        Basket4Rerolled?.Invoke(oldBall, newBall);
    }

    private bool CanChar2Reroll() =>
        _currentRound >= 4 && IsChar2SkillAvailable && UndrawnBalls.Count > 0;

    /// <summary>
    /// Performs the next shuffle: draws balls from pool into the next basket.
    /// In Extended mode, round 0 hides the 4th ball (revealed only via Char1 skill).
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
        int ballsToDraw = drawCount;

        // In extended mode, round 0 hides the 4th ball (Char1 skill)
        bool hideFourthBall = GameMode == GameMode.Extended && _currentRound == 0;

        for (int i = 0; i < ballsToDraw; i++)
        {
            int idx = Random.Shared.Next(poolCopy.Count);
            var ball = poolCopy[idx];
            poolCopy.RemoveAt(idx);

            // In extended mode round 0: skip firing event for the hidden 4th ball
            bool isHiddenBall = hideFourthBall && i == drawCount - 1;

            if (!isHiddenBall)
            {
                targetBasket.Add(ball);
                // Raise event for View to animate this ball
                BallAddedToBasket?.Invoke(ball, _currentRound);
            }
            else
            {
                // Store the hidden ball — it stays out of Basket1 collection
                _hiddenBall = ball;
            }

            // Remove from actual pool
            _pool.Remove(ball);

            // Notify UI that pool count changed (updates "remaining" label immediately)
            OnPropertyChanged(nameof(PoolCount));

            // Small delay between each ball appearing (for staggered animation)
            await Task.Delay(150);
        }

        _currentRound++;
        OnPropertyChanged(nameof(CurrentRoundDisplay));
        OnPropertyChanged(nameof(CanShuffle));

        // After round 4 (or round 3 in extended mode), enable Char2 skill
        if (_currentRound >= 4)
        {
            IsChar2SkillAvailable = true;
        }

        // After all rounds, remaining balls go to UndrawnBalls
        if (_currentRound >= 4)
        {
            foreach (var ball in _pool)
            {
                UndrawnBalls.Add(ball);
            }
            _pool.Clear();
        }

        ShuffleCommand.NotifyCanExecuteChanged();
        Char2RerollCommand.NotifyCanExecuteChanged();
    }

    /// <summary>
    /// Resets the game: clears all baskets, refills the pool, resets round counter.
    /// Also resets character skill states.
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
        _hiddenBall = null;
        IsChar2SkillAvailable = false;

        OnPropertyChanged(nameof(CurrentRoundDisplay));
        OnPropertyChanged(nameof(CanShuffle));
        OnPropertyChanged(nameof(PoolCount));
        OnPropertyChanged(nameof(HiddenBall));
        ShuffleCommand.NotifyCanExecuteChanged();
        Char2RerollCommand.NotifyCanExecuteChanged();
    }
}