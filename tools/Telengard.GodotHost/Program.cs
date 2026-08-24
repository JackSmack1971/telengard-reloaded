using System.Text.Json;
using Telengard.Content;
using Telengard.Core.Presentation;
using Telengard.Core.Simulation;
using Telengard.Core.Events;

namespace Telengard.GodotHost;

internal static class Program
{
    private static int Main(string[] args)
    {
        try
        {
            var contentRoot = ReadContentRoot(args);
            var pack = ContentPackLoader.Load(contentRoot);
            var playerId = Guid.Parse("00000000-0000-0000-0000-000000000120");
            var initial = GameState.Create(120, playerId: playerId);
            var provider = new RolledCharacterCreationProvider(
                new RolledCharacterCreationConfiguration(
                    "godot-bootstrap-v1",
                    Enumerable.Repeat(new RolledAttributeRange(3, 18), 6)));
            var character = provider.Create(initial, new CharacterCreationRequest(CharacterCreationMode.Rolled));
            var result = NewGameSetupResolver.Create(new NewGameSetupRequest(120, GameMode.Classic, character));
            var eventBus = new DomainEventBus();
            var committedEvents = new List<IDomainEvent>();
            using var subscription = eventBus.Subscribe<IDomainEvent>(committedEvents.Add);
            _ = new CommandDispatcher(result.State, eventBus);
            eventBus.Publish(result.Events);
            var frame = ModernRenderer.Create(PresentationStateAdapter.Create(result.State), committedEvents);
            Console.WriteLine(JsonSerializer.Serialize(new
            {
                content_version = pack.ContentVersion,
                content_counts = new { monsters = pack.Monsters.Count, items = pack.Items.Count, spells = pack.Spells.Count, features = pack.Features.Count },
                committed_event_count = committedEvents.Count,
                frame = FrameJson(frame)
            }));
            return 0;
        }
        catch (Exception exception) when (exception is ArgumentException or IOException or InvalidDataException)
        {
            Console.Error.WriteLine(exception.Message);
            return 1;
        }
    }

    private static string ReadContentRoot(string[] args)
    {
        var index = Array.IndexOf(args, "--content-root");
        if (index < 0 || index + 1 >= args.Length) throw new ArgumentException("Usage: --content-root <path>.");
        return Path.GetFullPath(args[index + 1]);
    }

    private static object FrameJson(ModernRenderFrame frame) => new
    {
        scene = frame.Scene.ToString().ToLowerInvariant(),
        player_position = Position(frame.PlayerPosition),
        environment = new { dynamic_lighting = frame.Environment.DynamicLighting, atmospheric_effects = frame.Environment.AtmosphericEffects },
        tiles = frame.Tiles.Select(tile => new { position = Position(tile.Position), knowledge = tile.Knowledge.ToString().ToLowerInvariant() }),
        features = frame.Features.Select(feature => new { instance_id = feature.InstanceId, definition_id = feature.DefinitionId, position = Position(feature.Position), activation_count = feature.ActivationCount }),
        hud = new { player_id = frame.Hud.PlayerId, level = frame.Hud.Level, hit_points = frame.Hud.HitPoints, max_hit_points = frame.Hud.MaxHitPoints, spell_power = frame.Hud.SpellPower, max_spell_power = frame.Hud.MaxSpellPower, carried_gold = frame.Hud.CarriedGold, secured_gold = frame.Hud.SecuredGold, alive = frame.Hud.Alive }
    };

    private static object Position(DungeonPosition position) => new { floor = position.Floor, x = position.X, y = position.Y };
}
