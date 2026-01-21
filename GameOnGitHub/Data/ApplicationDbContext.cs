using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace GameOnGitHub.Data
{
    public GameDbContext(DbContextOptions<GameDbContext> options) : base(options) { }

    public DbSet<PlayerSession> PlayerSessions => Set<PlayerSession>();
    public DbSet<Room> Rooms => Set<Room>();
    public DbSet<RoomPlayer> RoomPlayers => Set<RoomPlayer>();
    public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();

    public DbSet<BoardSpaceDefinition> BoardSpaces => Set<BoardSpaceDefinition>();
    public DbSet<CardDefinition> Cards => Set<CardDefinition>();

    public DbSet<Game> Games => Set<Game>();
    public DbSet<GamePlayer> GamePlayers => Set<GamePlayer>();
    public DbSet<GameSpaceState> GameSpaceStates => Set<GameSpaceState>();
    public DbSet<GameDeckState> GameDeckStates => Set<GameDeckState>();
    public DbSet<GameDeckCardOrder> GameDeckCardOrders => Set<GameDeckCardOrder>();

    public DbSet<DiceRoll> DiceRolls => Set<DiceRoll>();
    public DbSet<GameEventLog> GameEvents => Set<GameEventLog>();

    public DbSet<TradeOffer> TradeOffers => Set<TradeOffer>();
    public DbSet<TradeOfferProperty> TradeOfferProperties => Set<TradeOfferProperty>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
                    base.OnModelCreating(modelBuilder);

        // Room.Code must be unique
        modelBuilder.Entity<Room>()
            .HasIndex(r => r.Code)
            .IsUnique(); // unique index pattern :contentReference[oaicite:1]{index=1}

        // RoomPlayer composite key
        modelBuilder.Entity<RoomPlayer>()
            .HasKey(rp => new { rp.RoomId, rp.PlayerSessionId });

        modelBuilder.Entity<RoomPlayer>()
            .HasOne(rp => rp.Room)
            .WithMany(r => r.Players)
            .HasForeignKey(rp => rp.RoomId)
            .OnDelete(DeleteBehavior.Cascade); // cascade delete guidance :contentReference[oaicite:2]{index=2}

        modelBuilder.Entity<RoomPlayer>()
            .HasOne(rp => rp.Player)
            .WithMany(p => p.RoomMemberships)
            .HasForeignKey(rp => rp.PlayerSessionId)
            .OnDelete(DeleteBehavior.Cascade);

        // Important: avoid SQL Server "multiple cascade paths" by restricting Owner FK
        modelBuilder.Entity<Room>()
            .HasOne(r => r.Owner)
            .WithMany()
            .HasForeignKey(r => r.OwnerPlayerSessionId)
            .OnDelete(DeleteBehavior.Restrict);

        // Chat
        modelBuilder.Entity<ChatMessage>()
            .HasIndex(m => new { m.RoomId, m.SentAtUtc });

        modelBuilder.Entity<ChatMessage>()
            .HasOne(m => m.Room)
            .WithMany(r => r.ChatMessages)
            .HasForeignKey(m => m.RoomId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ChatMessage>()
            .HasOne(m => m.Player)
            .WithMany(p => p.ChatMessages)
            .HasForeignKey(m => m.PlayerSessionId)
            .OnDelete(DeleteBehavior.SetNull);

        // Board spaces
        modelBuilder.Entity<BoardSpaceDefinition>()
            .HasIndex(b => b.Index)
            .IsUnique();

        // Cards uniqueness inside a deck (optional but helpful)
        modelBuilder.Entity<CardDefinition>()
            .HasIndex(c => new { c.DeckType, c.Sequence })
            .IsUnique();

        // One-to-one Room -> Game (optional until started)
        modelBuilder.Entity<Room>()
            .HasOne(r => r.Game)
            .WithOne(g => g.Room)
            .HasForeignKey<Game>(g => g.RoomId)
            .OnDelete(DeleteBehavior.Cascade);

        // GamePlayer composite key
        modelBuilder.Entity<GamePlayer>()
            .HasKey(gp => new { gp.GameId, gp.PlayerSessionId });

        modelBuilder.Entity<GamePlayer>()
            .HasOne(gp => gp.Game)
            .WithMany(g => g.Players)
            .HasForeignKey(gp => gp.GameId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<GamePlayer>()
            .HasOne(gp => gp.Player)
            .WithMany()
            .HasForeignKey(gp => gp.PlayerSessionId)
            .OnDelete(DeleteBehavior.Restrict);

        // Space state composite key
        modelBuilder.Entity<GameSpaceState>()
            .HasKey(s => new { s.GameId, s.BoardSpaceId });

        modelBuilder.Entity<GameSpaceState>()
            .HasOne(s => s.Game)
            .WithMany(g => g.SpaceStates)
            .HasForeignKey(s => s.GameId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<GameSpaceState>()
            .HasOne(s => s.BoardSpace)
            .WithMany()
            .HasForeignKey(s => s.BoardSpaceId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<GameSpaceState>()
            .HasOne(s => s.Owner)
            .WithMany()
            .HasForeignKey(s => s.OwnerPlayerSessionId)
            .OnDelete(DeleteBehavior.SetNull);

        // Deck state composite key
        modelBuilder.Entity<GameDeckState>()
            .HasKey(d => new { d.GameId, d.DeckType });

        modelBuilder.Entity<GameDeckState>()
            .HasOne(d => d.Game)
            .WithMany(g => g.DeckStates)
            .HasForeignKey(d => d.GameId)
            .OnDelete(DeleteBehavior.Cascade);

        // Deck order composite key
        modelBuilder.Entity<GameDeckCardOrder>()
            .HasKey(o => new { o.GameId, o.DeckType, o.OrderIndex });

        modelBuilder.Entity<GameDeckCardOrder>()
            .HasOne(o => o.Game)
            .WithMany(g => g.DeckOrder)
            .HasForeignKey(o => o.GameId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<GameDeckCardOrder>()
            .HasOne(o => o.Card)
            .WithMany()
            .HasForeignKey(o => o.CardDefinitionId)
            .OnDelete(DeleteBehavior.Restrict);

        // Dice rolls
        modelBuilder.Entity<DiceRoll>()
            .HasIndex(r => new { r.GameId, r.TurnNumber });

        // Events
        modelBuilder.Entity<GameEventLog>()
            .HasIndex(e => new { e.GameId, e.CreatedAtUtc });

        // Trades
        modelBuilder.Entity<TradeOffer>()
            .HasIndex(t => new { t.GameId, t.Status });

        modelBuilder.Entity<TradeOfferProperty>()
            .HasOne(p => p.TradeOffer)
            .WithMany(t => t.Properties)
            .HasForeignKey(p => p.TradeOfferId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
