using System.Collections.Generic;

namespace PirateGame.Persistence
{
    // Schema 1 save envelope. Plain serializer DTOs only: stable IDs and values,
    // never scene references or type names. SaveMapper validates every field
    // before any domain state is constructed from it.
    public sealed class SaveFileDto
    {
        public string format;
        public int schema;
        public string written;
        public long revision;
        public string request;
        public string command;
        public CampaignDto campaign;
        public ExpeditionDto expedition;
        public List<string> committedRequests;
    }

    public sealed class CampaignDto
    {
        public string hull;
        public string currentHub;
        public string lastSafeHub;
        public Dictionary<string, int> bank;
        public Dictionary<string, int> tiers;
        public Dictionary<string, string> ownedEquipment;
        public Dictionary<string, string> loadout;
        public Dictionary<string, HubDto> hubs;
        public List<string> unlocks;
        public Dictionary<string, string> resolvedExpeditions;
    }

    public sealed class HubDto
    {
        public bool discovered;
        public bool activated;
        public List<string> storyFlags;
    }

    public sealed class ExpeditionDto
    {
        public string id;
        public int seed;
        public string rngState;
        public long tick;
        public double health;
        public double speed;
        public PositionDto position;
        public Dictionary<string, int> cargo;
        public List<ModifierDto> modifiers;
        public Dictionary<string, double> cooldowns;
        public Dictionary<string, string> encounters;
        public List<EntityDto> entities;
    }

    public sealed class PositionDto
    {
        public string region;
        public double x;
        public double z;
    }

    public sealed class ModifierDto
    {
        public string stat;
        public string operation;
        public double value;
    }

    public sealed class EntityDto
    {
        public string authoredId;
        public string expeditionId;
        public string spawnId;
        public string definition;
        public PositionDto position;
        public double health;
        public bool defeated;
        public Dictionary<string, int> loot;
        public Dictionary<string, double> cooldowns;
        public string behavior;
    }
}
