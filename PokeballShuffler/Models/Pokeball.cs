using Microsoft.Maui.Graphics;

namespace PokeballShuffler.Models;

/// <summary>
/// Represents a single Pokeball instance with its type and image source.
/// </summary>
public class Pokeball
{
    public BallType BallType { get; }
    public string ImageSource { get; }

    // Color used for UI accent/styling based on ball type
    public Color AccentColor => BallType switch
    {
        BallType.PokeBall => Colors.Red,
        BallType.PremierBall => Colors.White,
        BallType.GreatBall => Colors.Blue,
        BallType.UltraBall => Colors.Black,
        BallType.MasterBall => Colors.Purple,
        _ => Colors.Gray
    };

    public string DisplayName => BallType switch
    {
        BallType.PokeBall => "Poké Ball",
        BallType.PremierBall => "Premier Ball",
        BallType.GreatBall => "Great Ball",
        BallType.UltraBall => "Ultra Ball",
        BallType.MasterBall => "Master Ball",
        _ => "Unknown"
    };

    public Pokeball(BallType ballType)
    {
        BallType = ballType;
        // Map BallType enum to the image filename stored in Resources/Images
        ImageSource = ballType switch
        {
            BallType.PokeBall => "poke_ball.png",
            BallType.PremierBall => "premier_ball.png",
            BallType.GreatBall => "great_ball.png",
            BallType.UltraBall => "ultra_ball.png",
            BallType.MasterBall => "master_ball.png",
            _ => "poke_ball.png"
        };
    }
}