using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PirateGame.Core;

namespace PirateGame.Persistence
{
    // Malformed, truncated or structurally invalid save content. Loading reports it
    // as a recoverable error; it never becomes a substituted or partial snapshot.
    public sealed class SaveFormatException : Exception
    {
        public bool Unsupported { get; }
        public SaveFormatException(string message, bool unsupported = false, Exception inner = null) : base(message, inner)
        { Unsupported = unsupported; }
    }

    public static class SaveCodec
    {
        public const string Format = "pirate-prototype-save";
        public const int Schema = 1;

        private static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.None,
            DateParseHandling = DateParseHandling.None,
            FloatParseHandling = FloatParseHandling.Double,
            MissingMemberHandling = MissingMemberHandling.Ignore,
            NullValueHandling = NullValueHandling.Include,
            MaxDepth = 32,
            Formatting = Formatting.Indented,
            Culture = CultureInfo.InvariantCulture
        };

        public static byte[] Encode(SaveFileDto dto) =>
            new UTF8Encoding(false).GetBytes(JsonConvert.SerializeObject(dto, Settings));

        // Reads the envelope before binding DTOs so a newer or foreign file is
        // reported as unsupported rather than half-read by an older schema.
        public static SaveFileDto Decode(byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0) throw new SaveFormatException("Save file is empty.");
            try
            {
                string text = new UTF8Encoding(false, true).GetString(bytes);
                using (var reader = new JsonTextReader(new StringReader(text)) { DateParseHandling = DateParseHandling.None, MaxDepth = 32 })
                {
                    var token = JToken.ReadFrom(reader);
                    if (reader.Read()) throw new SaveFormatException("Trailing content after the save document.");
                    if (!(token is JObject root)) throw new SaveFormatException("Save root must be an object.");
                    if ((string)root["format"] != Format) throw new SaveFormatException("Not a Pirate Prototype save.", true);
                    var schema = root["schema"];
                    if (schema == null || schema.Type != JTokenType.Integer) throw new SaveFormatException("Missing save schema.");
                    int version = schema.Value<int>();
                    if (version != Schema)
                        // Future schema changes add explicit migrations and prior-schema fixtures here.
                        throw new SaveFormatException("Unsupported save schema " + version + "; this build reads schema " + Schema + ".", true);
                    return root.ToObject<SaveFileDto>(JsonSerializer.Create(Settings));
                }
            }
            catch (SaveFormatException) { throw; }
            catch (Exception e) when (e is JsonException || e is ArgumentException || e is FormatException ||
                                      e is InvalidCastException || e is OverflowException)
            { throw new SaveFormatException("Save file is malformed: " + e.Message, false, e); }
        }
    }

    public static class SaveMapper
    {
        public static SaveFileDto ToDto(SessionSnapshot snapshot, Guid request, string command, DateTime writtenUtc)
        {
            if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));
            var c = snapshot.Campaign;
            return new SaveFileDto
            {
                format = SaveCodec.Format,
                schema = SaveCodec.Schema,
                written = writtenUtc.ToUniversalTime().ToString("o", CultureInfo.InvariantCulture),
                revision = snapshot.Revision,
                request = request == Guid.Empty ? null : request.ToString("D"),
                command = command,
                campaign = new CampaignDto
                {
                    hull = c.HullId,
                    currentHub = c.CurrentHub,
                    lastSafeHub = c.LastSafeHub,
                    bank = Sorted(c.Bank),
                    tiers = Sorted(c.Tiers),
                    ownedEquipment = Sorted(c.OwnedEquipment),
                    loadout = Sorted(c.Loadout),
                    hubs = Sorted(c.Hubs.ToDictionary(p => p.Key, p => new HubDto
                    {
                        discovered = p.Value.Discovered,
                        activated = p.Value.Activated,
                        storyFlags = p.Value.StoryFlags.ToList()
                    })),
                    unlocks = c.Unlocks.ToList(),
                    resolvedExpeditions = c.ResolvedExpeditions.OrderBy(p => p.Key.ToString("D"), StringComparer.Ordinal)
                        .ToDictionary(p => p.Key.ToString("D"), p => p.Value.ToString())
                },
                expedition = snapshot.Expedition == null ? null : ToDto(snapshot.Expedition),
                committedRequests = snapshot.CommittedRequests.Select(g => g.ToString("D")).ToList()
            };
        }

        private static ExpeditionDto ToDto(ExpeditionState e) => new ExpeditionDto
        {
            id = e.Id.ToString("D"),
            seed = e.Seed,
            rngState = e.RngState,
            tick = e.Tick,
            health = e.Health,
            speed = e.Speed,
            position = ToDto(e.Position),
            cargo = Sorted(e.Cargo),
            modifiers = e.Modifiers.Select(m => new ModifierDto { stat = m.StatId, operation = m.Operation.ToString(), value = m.Value }).ToList(),
            cooldowns = Sorted(e.Cooldowns),
            encounters = Sorted(e.Encounters),
            entities = e.Entities.Values.OrderBy(x => x.Id.ToString(), StringComparer.Ordinal).Select(ToDto).ToList()
        };

        private static EntityDto ToDto(EntityState s) => new EntityDto
        {
            authoredId = s.Id.AuthoredId,
            expeditionId = s.Id.AuthoredId == null ? s.Id.ExpeditionId.ToString("D") : null,
            spawnId = s.Id.SpawnId,
            definition = s.DefinitionId,
            position = ToDto(s.Position),
            health = s.Health,
            defeated = s.Defeated,
            loot = Sorted(s.Loot),
            cooldowns = Sorted(s.Cooldowns),
            behavior = s.BehaviorState
        };

        private static PositionDto ToDto(SeaPosition p) => new PositionDto { region = p.RegionId, x = p.X, z = p.Z };

        private static Dictionary<string, T> Sorted<T>(IEnumerable<KeyValuePair<string, T>> source) =>
            source.OrderBy(p => p.Key, StringComparer.Ordinal).ToDictionary(p => p.Key, p => p.Value, StringComparer.Ordinal);

        // Structural validation only. Content validation (known IDs, loadouts,
        // capacities) remains CampaignSession.ValidateSnapshot's responsibility.
        public static SessionSnapshot FromDto(SaveFileDto dto)
        {
            try
            {
                Require(dto != null, "Missing save document.");
                Require(dto.format == SaveCodec.Format, "Not a Pirate Prototype save.");
                Require(dto.schema == SaveCodec.Schema, "Unsupported save schema.");
                Require(dto.revision >= 0, "Invalid revision.");
                var c = Required(dto.campaign, "campaign");
                var hubs = Required(c.hubs, "campaign.hubs").ToDictionary(p => p.Key, p =>
                {
                    var hub = Required(p.Value, "hub " + p.Key);
                    return new HubState(hub.discovered, hub.activated, Required(hub.storyFlags, "hub flags"));
                });
                var resolved = Required(c.resolvedExpeditions, "campaign.resolvedExpeditions").Select(p =>
                    new KeyValuePair<Guid, Outcome>(ParseGuid(p.Key, "resolved expedition"), ParseEnum<Outcome>(p.Value, "outcome")));
                var campaign = new CampaignState(c.hull, c.currentHub, c.lastSafeHub,
                    Required(c.bank, "campaign.bank"), Required(c.tiers, "campaign.tiers"),
                    Required(c.ownedEquipment, "campaign.ownedEquipment"), Required(c.loadout, "campaign.loadout"),
                    hubs, Required(c.unlocks, "campaign.unlocks"), resolved);
                var expedition = dto.expedition == null ? null : FromDto(dto.expedition);
                var requests = Required(dto.committedRequests, "committedRequests").Select(r => ParseGuid(r, "committed request")).ToArray();
                return new SessionSnapshot(dto.revision, campaign, expedition, requests);
            }
            catch (SaveFormatException) { throw; }
            catch (Exception e) when (e is ArgumentException || e is InvalidOperationException || e is OverflowException)
            { throw new SaveFormatException("Save content is invalid: " + e.Message, false, e); }
        }

        private static ExpeditionState FromDto(ExpeditionDto e)
        {
            var modifiers = Required(e.modifiers, "expedition.modifiers").Select(m =>
            {
                Require(m != null, "Missing modifier.");
                return new StatModifier(m.stat, ParseEnum<ModifierOperation>(m.operation, "modifier operation"), m.value);
            });
            var entities = Required(e.entities, "expedition.entities").Select(FromDto).ToArray();
            Require(entities.Select(x => x.Id).Distinct().Count() == entities.Length, "Duplicate entity identity.");
            return new ExpeditionState(ParseGuid(e.id, "expedition"), e.seed, e.rngState, e.tick, e.health, e.speed,
                FromDto(e.position), Required(e.cargo, "expedition.cargo"), modifiers,
                Required(e.cooldowns, "expedition.cooldowns"), Required(e.encounters, "expedition.encounters"), entities);
        }

        private static EntityState FromDto(EntityDto s)
        {
            Require(s != null, "Missing entity.");
            EntityId id;
            if (s.authoredId != null)
            {
                Require(s.expeditionId == null && s.spawnId == null, "Entity has both authored and generated identity.");
                id = EntityId.Authored(s.authoredId);
            }
            else id = EntityId.Generated(ParseGuid(s.expeditionId, "entity expedition"), s.spawnId);
            return new EntityState(id, s.definition, FromDto(s.position), s.health, s.defeated,
                Required(s.loot, "entity loot"), Required(s.cooldowns, "entity cooldowns"), s.behavior);
        }

        private static SeaPosition FromDto(PositionDto p)
        {
            Require(p != null, "Missing position.");
            return new SeaPosition(p.region, p.x, p.z);
        }

        private static Guid ParseGuid(string value, string field)
        {
            if (value == null || !Guid.TryParseExact(value, "D", out var guid) || guid == Guid.Empty)
                throw new SaveFormatException("Invalid " + field + " identity.");
            return guid;
        }

        private static T ParseEnum<T>(string value, string field) where T : struct
        {
            if (value == null || !Enum.TryParse<T>(value, false, out var parsed) || !Enum.IsDefined(typeof(T), parsed) ||
                value.Trim() != value || char.IsDigit(value[0]))
                throw new SaveFormatException("Invalid " + field + ".");
            return parsed;
        }

        private static T Required<T>(T value, string field) where T : class =>
            value ?? throw new SaveFormatException("Missing " + field + ".");

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new SaveFormatException(message);
        }
    }
}
