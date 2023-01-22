using CodeDeck.PluginAbstractions;
using System;
using System.Threading;
using System.Threading.Tasks;


/// <summary>
/// A wrapper class for the Plugin, this class MUST extend the CodeDeckPlugin class
/// This class can be used as a container for static fields that are shared between Tile classes
/// This class is never instantiated, but you may use a static constructor to initialize static fields:
/// https://learn.microsoft.com/en-us/dotnet/csharp/programming-guide/classes-and-structs/static-constructors
/// </summary>
public class Template : CodeDeckPlugin
{
    /// <summary>
    /// An example of a static field that can be shared between the Tile classes
    /// </summary>
    private static DateTime _dateTimeNow;

    /// <summary>
    /// A static constructor that initializes shared static fields
    /// </summary>
    static Template()
    {
        _dateTimeNow = DateTime.Now;
    }


    /// <summary>
    /// An example of how to implement a new Tile, a Tile class MUST extend the Tile class
    /// </summary>
    public class TemplateTileOne : Tile
    {
        /// <summary>
        /// A property annotated with the SettingAttribute
        /// This property will be automatically bound to the appropriate setting from the
        /// JSON configuration file.
        /// </summary>
        [Setting] public int? Counter { get; set; }

        /// <summary>
        /// The constructor for a Tile
        /// The constructor is called whenever a Tile is instantiated, but it's recommended to
        /// use the Init-method below instead. The Init-method supports cancellation, which is
        /// important to make sure that the plugin system can cleanly remove Tile-instances as
        /// needed.
        /// The constructor may be used for assigning private fields, but DO NOT start long
        /// running tasks in the constructor!
        /// </summary>
        public TemplateTileOne()
        {
            Counter = 0;
        }

        /// <summary>
        /// This method is called by the plugin system when this Tile is instantiated.
        /// Use this method as you would normally use the constructor. Make sure to forward
        /// the CancellationToken to any long running tasks or background tasks.
        /// </summary>
        /// <param name="cancellationToken"></param>
        public override Task Init(CancellationToken cancellationToken)
        {
            Text = $"TileOne\n{_dateTimeNow.ToShortTimeString()}\n{Counter}";
            return base.Init(cancellationToken);
        }

        /// <summary>
        /// This method is called by the plugin system when a key associated with this
        /// Tile is pressed
        /// </summary>
        /// <param name="cancellationToken"></param>
        public override Task OnTilePressDown(CancellationToken cancellationToken)
        {
            Counter++;
            Text = $"TileOne\n{_dateTimeNow.ToShortTimeString()}\n{Counter}";
            return base.OnTilePressDown(cancellationToken);
        }

        /// <summary>
        /// This method is called by the plugin system when a key associated with this
        /// Tile is released
        /// </summary>
        /// <param name="cancellationToken"></param>
        public override Task OnTilePressUp(CancellationToken cancellationToken)
        {
            return base.OnTilePressUp(cancellationToken);
        }

        /// <summary>
        /// This method is called by the plugin system when a Tile instance is removed,
        /// make sure to clean up any resources here.
        /// </summary>
        public override Task DeInit()
        {
            return base.DeInit();
        }
    }


    /// <summary>
    /// A plugin my contain multiple Tile types
    /// </summary>
    public class TemplateTileTwo : Tile
    {
        public override Task Init(CancellationToken cancellationToken)
        {
            Text = "TileTwo";
            return base.Init(cancellationToken);
        }
    }
}
