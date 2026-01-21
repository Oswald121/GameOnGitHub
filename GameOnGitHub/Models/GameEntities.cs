using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GameOnGitHub.Models; // <- change to your project namespace

public enum RoomStatus : byte
{
    Waiting = 0,
    Started = 1,
    Finished = 2
}

public enum GameStatus : byte
{
    InProgress = 0,
    Finished = 1,
    Aborted = 2
}

public enum SpaceType : byte
{
    Go = 0,
    Property = 1,
    Railroad = 2,
    Utility = 3,
    Chance = 4,
    CommunityChest = 5,
    Tax = 6,
    Jail = 7,
    FreeParking = 8,
    GoToJail = 9
}

public enum ColorGroup : byte
{
    None = 0,
    Brown = 1,
    LightBlue = 2,
    Pink = 3,
    Orange = 4,
    Red = 5,
    Yellow = 6,
    Green = 7,
    DarkBlue = 8
}

public enum DeckType : byte
{
    Chance = 0,
    CommunityChest = 1
}

public enum TradeOfferStatus : byte
{
    Pending = 0,
    Accepted = 1,
    Rejected = 2,
    Cancelled = 3,
    Expired = 4
}

/// <summary>
/// A lightweight “identity” for the player while they stay in the game.
/// You can create this when user enters name, store SessionToken in cookie.
/// </summary>
public class PlayerSession
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required, MaxLength(32)]
    public string DisplayName { get; set; } = default!;

    // Put a random token here and store it in a cookie so refresh/reconnect keeps identity.
    [Required, MaxLength(64)]
    public string SessionToken { get; set; } = default!;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime LastSeenAtUtc { get; set; } = DateTime.UtcNow;

    public ICollection<RoomPlayer> RoomMemberships { get; set; } = new List<RoomPlayer>();
    public ICollection<ChatMessage> ChatMessages { get; set; } = new List<ChatMessage>();
}

/// <summary>
/// Lobby concept: owner creates it, shares Code, others join. Max 8.
/// </summary>
public class Room
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required, MaxLength(10)]
    public string Code { get; set; } = default!; // unique

    public Guid OwnerPlayerSessionId { get; set; }
    public PlayerSession Owner { get; set; } = default!;

    public RoomStatus Status { get; set; } = RoomStatus.Waiting;

    public byte MaxPlayers { get; set; } = 8;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? StartedAtUtc { get; set; }
    public DateTime? FinishedAtUtc { get; set; }

    public ICollection<RoomPlayer> Players { get; set; } = new List<RoomPlayer>();
    public ICollection<ChatMessage> ChatMessages { get; set; } = new List<ChatMessage>();

    public Game? Game { get; set; } // 1:1 after start
}

/// <summary>
/// Many-to-many Room <-> PlayerSession with extra fields (seat/order/token).
/// Composite key (RoomId, PlayerSessionId).
/// </summary>
public class RoomPlayer
{
    public Guid RoomId { get; set; }
    public Room Room { get; set; } = default!;

    public Guid PlayerSessionId { get; set; }
    public PlayerSession Player { get; set; } = default!;

    public int JoinOrder { get; set; } // determines turn order seed, seat, etc.
    [MaxLength(32)]
    public string? Token { get; set; } // e.g. “Car”, “Hat” (or your own set)

    public bool IsOwner { get; set; }
    public bool IsConnected { get; set; } = true;

    public DateTime JoinedAtUtc { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Always-on chat (room-wide). Keep PlayerName denormalized so messages survive if session is cleaned.
/// </summary>
public class ChatMessage
{
    public long Id { get; set; }

    public Guid RoomId { get; set; }
    public Room Room { get; set; } = default!;

    public Guid? PlayerSessionId { get; set; }
    public PlayerSession? Player { get; set; }

    [Required, MaxLength(32)]
    public string PlayerName { get; set; } = default!;

    [Required, MaxLength(500)]
    public string Message { get; set; } = default!;

    public DateTime SentAtUtc { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Static definition for the board. Keep this forever; seed once.
/// You can also load/seed from JSON later for different editions.
/// </summary>
public class BoardSpaceDefinition
{
    public int Id { get; set; } // 1..40 or 0..39 (your choice)
    public int Index { get; set; } // MUST be unique: 0..39 around the board

    [Required, MaxLength(64)]
    public string Name { get; set; } = default!;

    public SpaceType SpaceType { get; set; }

    public ColorGroup ColorGroup { get; set; } = ColorGroup.None;

    // Buyable spaces:
    public int? PurchasePrice { get; set; }
    public int? MortgageValue { get; set; }
    public int? HouseCost { get; set; }

    // Rent table (null for non-properties):
    public int? Rent0 { get; set; }
    public int? Rent1 { get; set; }
    public int? Rent2 { get; set; }
    public int? Rent3 { get; set; }
    public int? Rent4 { get; set; }
    public int? RentHotel { get; set; }

    // Tax spaces:
    public int? TaxAmount { get; set; }
}

/// <summary>
/// Static definitions for Chance/Community Chest.
/// ActionJson lets you implement rules without schema changes.
/// </summary>
public class CardDefinition
{
    public int Id { get; set; }

    public DeckType DeckType { get; set; }

    // Useful if you want to keep official ordering per edition (optional)
    public int Sequence { get; set; }

    [Required, MaxLength(256)]
    public string Text { get; set; } = default!;

    [MaxLength(64)]
    public string? ActionCode { get; set; } // e.g. MOVE_TO, PAY_BANK, COLLECT, JAIL_FREE, etc.

    public string? ActionJson { get; set; } // store details: { "to": 0 } etc.
}

/// <summary>
/// One match per room when started.
/// </summary>
public class Game
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid RoomId { get; set; }
    public Room Room { get; set; } = default!;

    public GameStatus Status { get; set; } = GameStatus.InProgress;

    public int TurnNumber { get; set; } = 1;

    public Guid? CurrentPlayerSessionId { get; set; }

    public DateTime TurnStartedAtUtc { get; set; } = DateTime.UtcNow;

    public Guid? WinnerPlayerSessionId { get; set; }

    [Timestamp]
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    public ICollection<GamePlayer> Players { get; set; } = new List<GamePlayer>();
    public ICollection<GameSpaceState> SpaceStates { get; set; } = new List<GameSpaceState>();
    public ICollection<GameDeckState> DeckStates { get; set; } = new List<GameDeckState>();
    public ICollection<GameDeckCardOrder> DeckOrder { get; set; } = new List<GameDeckCardOrder>();
    public ICollection<DiceRoll> DiceRolls { get; set; } = new List<DiceRoll>();
    public ICollection<TradeOffer> TradeOffers { get; set; } = new List<TradeOffer>();
    public ICollection<GameEventLog> Events { get; set; } = new List<GameEventLog>();
}

/// <summary>
/// Composite key (GameId, PlayerSessionId).
/// Stores everything needed for “live” UI: money, position, jail, autoroll strikes, etc.
/// </summary>
public class GamePlayer
{
    public Guid GameId { get; set; }
    public Game Game { get; set; } = default!;

    public Guid PlayerSessionId { get; set; }
    public PlayerSession Player { get; set; } = default!;

    public int TurnOrder { get; set; } // 0..N-1

    public int Cash { get; set; } = 1500;

    public int Position { get; set; } = 0;

    public bool InJail { get; set; }
    public int JailTurns { get; set; }

    public int ConsecutiveDoubles { get; set; }

    // Your “1 minute to roll or auto-roll” rule + “5 auto-rolls => bankruptcy”
    public int AutoRollStrikes { get; set; }

    public bool IsBankrupt { get; set; }
    public DateTime? BankruptAtUtc { get; set; }

    [MaxLength(32)]
    public string? Token { get; set; }

    [Timestamp]
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}

/// <summary>
/// Game-specific state for each board space (owner, houses, mortgage).
/// Composite key (GameId, BoardSpaceId).
/// </summary>
public class GameSpaceState
{
    public Guid GameId { get; set; }
    public Game Game { get; set; } = default!;

    public int BoardSpaceId { get; set; }
    public BoardSpaceDefinition BoardSpace { get; set; } = default!;

    public Guid? OwnerPlayerSessionId { get; set; }
    public PlayerSession? Owner { get; set; }

    public int Houses { get; set; } // 0..4
    public bool HasHotel { get; set; }
    public bool IsMortgaged { get; set; }

    [Timestamp]
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}

/// <summary>
/// Pointer for each deck (next draw index).
/// Composite key (GameId, DeckType).
/// </summary>
public class GameDeckState
{
    public Guid GameId { get; set; }
    public Game Game { get; set; } = default!;

    public DeckType DeckType { get; set; }

    public int NextDrawIndex { get; set; } = 0;
}

/// <summary>
/// Stores the deck order per game.
/// Composite key (GameId, DeckType, OrderIndex).
/// </summary>
public class GameDeckCardOrder
{
    public Guid GameId { get; set; }
    public Game Game { get; set; } = default!;

    public DeckType DeckType { get; set; }

    public int OrderIndex { get; set; } // 0..N-1

    public int CardDefinitionId { get; set; }
    public CardDefinition Card { get; set; } = default!;
}

public class DiceRoll
{
    public long Id { get; set; }

    public Guid GameId { get; set; }
    public Game Game { get; set; } = default!;

    public Guid PlayerSessionId { get; set; }
    public PlayerSession Player { get; set; } = default!;

    public int TurnNumber { get; set; }

    public byte Die1 { get; set; }
    public byte Die2 { get; set; }

    public bool IsAutoRoll { get; set; }

    public DateTime RolledAtUtc { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// For “everything is live” notifications. Store structured JSON so clients can replay/sync.
/// </summary>
public class GameEventLog
{
    public long Id { get; set; }

    public Guid GameId { get; set; }
    public Game Game { get; set; } = default!;

    [Required, MaxLength(64)]
    public string EventType { get; set; } = default!; // e.g. "PLAYER_MOVED", "BOUGHT_PROPERTY"

    public string? PayloadJson { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

public class TradeOffer
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid GameId { get; set; }
    public Game Game { get; set; } = default!;

    public Guid FromPlayerSessionId { get; set; }
    public PlayerSession FromPlayer { get; set; } = default!;

    public Guid ToPlayerSessionId { get; set; }
    public PlayerSession ToPlayer { get; set; } = default!;

    // Cash direction is explicit:
    // FromCash: amount FROM gives TO
    // ToCash: amount TO gives FROM
    public int FromCash { get; set; }
    public int ToCash { get; set; }

    public TradeOfferStatus Status { get; set; } = TradeOfferStatus.Pending;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? RespondedAtUtc { get; set; }

    public DateTime? ExpiresAtUtc { get; set; }

    public ICollection<TradeOfferProperty> Properties { get; set; } = new List<TradeOfferProperty>();
}

/// <summary>
/// Properties included in a trade.
/// If IsFromPlayerGives=true => property goes From -> To
/// else => property goes To -> From
/// </summary>
public class TradeOfferProperty
{
    public long Id { get; set; }

    public Guid TradeOfferId { get; set; }
    public TradeOffer TradeOffer { get; set; } = default!;

    public int BoardSpaceId { get; set; }
    public BoardSpaceDefinition BoardSpace { get; set; } = default!;

    public bool IsFromPlayerGives { get; set; }
}
