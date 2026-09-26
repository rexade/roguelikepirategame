using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using PirateGame.Core;
using PirateGame.Rules.Application;

namespace PirateGame.Persistence
{
    // Filesystem boundary, replaceable by tests to inject failures at each step.
    public interface ISaveFileSystem
    {
        bool Exists(string path);
        byte[] ReadAllBytes(string path);
        // Must not return before the bytes are flushed to the storage device.
        void WriteAllBytes(string path, byte[] bytes);
        // Same-volume replacement; the previous destination becomes the backup.
        void Replace(string source, string destination, string backup);
        void Move(string source, string destination);
        void CreateDirectory(string path);
    }

    public sealed class DiskFileSystem : ISaveFileSystem
    {
        public bool Exists(string path) => File.Exists(path);
        public byte[] ReadAllBytes(string path) => File.ReadAllBytes(path);
        public void WriteAllBytes(string path, byte[] bytes)
        {
            using (var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, 4096, FileOptions.WriteThrough))
            {
                stream.Write(bytes, 0, bytes.Length);
                stream.Flush(true);
            }
        }
        public void Replace(string source, string destination, string backup) => File.Replace(source, destination, backup, true);
        public void Move(string source, string destination) => File.Move(source, destination);
        public void CreateDirectory(string path) => Directory.CreateDirectory(path);
    }

    public enum SaveLoadStatus { NoSave, Loaded, RecoveredFromBackup, Unreadable }

    public sealed class SaveLoadResult
    {
        public SaveLoadStatus Status { get; }
        public SessionSnapshot Snapshot { get; }
        public string Detail { get; }
        public IReadOnlyList<string> PreservedFiles { get; }
        // Why the saved voyage was resolved as lost at sea on load (see SaveMigration); empty otherwise.
        public string Notice { get; }
        // The voyage that was dropped, for reporting (for example its lost cargo); null otherwise.
        public ExpeditionState AbandonedVoyage { get; }
        public SaveLoadResult(SaveLoadStatus status, SessionSnapshot snapshot, string detail, IReadOnlyList<string> preserved,
            string notice = null, ExpeditionState abandonedVoyage = null)
        {
            Status = status; Snapshot = snapshot; Detail = detail ?? ""; PreservedFiles = preserved ?? Array.Empty<string>();
            Notice = notice ?? ""; AbandonedVoyage = abandonedVoyage;
        }
        public bool HasCampaign => Snapshot != null;
    }

    // Single ordered writer for one campaign. Every commit writes a same-volume
    // temporary file, reads it back, then atomically replaces the main save and
    // keeps the previous committed snapshot as the backup. Unreadable or
    // unsupported files are preserved under new names, never overwritten.
    // An optional voyage check lets composition reject a saved voyage that the
    // current world cannot restore; such a voyage is resolved as lost at sea in
    // memory (the file is only rewritten by the next normal commit).
    public sealed class JsonSaveStore : ISaveStore
    {
        public const string MainName = "campaign.json";
        public const string BackupName = "campaign.backup.json";
        public const string TempName = "campaign.json.tmp";

        private readonly ISaveFileSystem files;
        private readonly DefinitionCatalog definitions;
        private readonly Func<DateTime> clock;
        private readonly Func<ExpeditionState, string> voyageProblem;
        private bool ready;
        private long committedRevision;
        private Guid committedRequest;

        public string Directory { get; }
        public string MainPath => Path.Combine(Directory, MainName);
        public string BackupPath => Path.Combine(Directory, BackupName);
        public string TempPath => Path.Combine(Directory, TempName);
        public bool HasSaveFiles => files.Exists(MainPath) || files.Exists(BackupPath);
        public long? CommittedRevision => ready ? committedRevision : (long?)null;

        // voyageProblem returns null or "" when a saved voyage can be restored, else the reason it cannot.
        public JsonSaveStore(string directory, DefinitionCatalog definitions, ISaveFileSystem files = null, Func<DateTime> clock = null,
            Func<ExpeditionState, string> voyageProblem = null)
        {
            if (string.IsNullOrWhiteSpace(directory)) throw new ArgumentException("A save directory is required.");
            Directory = Path.GetFullPath(directory);
            this.definitions = definitions ?? throw new ArgumentNullException(nameof(definitions));
            this.files = files ?? new DiskFileSystem();
            this.clock = clock ?? (() => DateTime.UtcNow);
            this.voyageProblem = voyageProblem;
        }

        public RuleResult Commit(SaveCandidate candidate)
        {
            if (candidate == null) return Failed("Missing save candidate.");
            if (!ready) return Failed("No campaign is loaded or initialized in this store.");
            long revision = candidate.Snapshot.Revision;
            // A retry after an acknowledged write (e.g. a lost acknowledgement) is idempotent.
            if (revision == committedRevision && candidate.RequestId == committedRequest) return new RuleResult();
            if (candidate.ExpectedRevision != committedRevision)
                return Failed("Stale candidate: expected revision " + candidate.ExpectedRevision + ", committed " + committedRevision + ".");
            var written = Write(candidate.Snapshot, candidate.RequestId, candidate.Command);
            if (!written.IsSuccess) return written;
            committedRevision = revision; committedRequest = candidate.RequestId;
            return written;
        }

        // Creates a new campaign file. Existing files are renamed aside first.
        public RuleResult Initialize(SessionSnapshot initial, out IReadOnlyList<string> preserved)
        {
            var moved = new List<string>(); preserved = moved;
            if (initial == null) return Failed("Missing initial snapshot.");
            var valid = CampaignSession.ValidateSnapshot(definitions, initial);
            if (!valid.IsSuccess) return valid;
            try
            {
                files.CreateDirectory(Directory);
                foreach (var path in new[] { MainPath, BackupPath })
                    if (files.Exists(path)) { var target = Aside(path, "replaced"); files.Move(path, target); moved.Add(target); }
            }
            catch (Exception e) when (IsStorage(e)) { return Failed(e.Message); }
            ready = false;
            var result = Write(initial, Guid.Empty, "NewCampaign");
            if (!result.IsSuccess) return result;
            ready = true; committedRevision = initial.Revision; committedRequest = Guid.Empty;
            return result;
        }

        public SaveLoadResult Load()
        {
            ready = false;
            if (!HasSaveFiles) return new SaveLoadResult(SaveLoadStatus.NoSave, null, "No saved campaign.", null);
            var problems = new List<string>();
            var main = TryRead(MainPath, problems);
            if (main != null) { Adopt(main); return new SaveLoadResult(SaveLoadStatus.Loaded, main.Snapshot, "", null, main.Notice, main.Abandoned); }
            var backup = TryRead(BackupPath, problems);
            if (backup == null)
                return new SaveLoadResult(SaveLoadStatus.Unreadable, null, string.Join(" ", problems), null);
            var preserved = new List<string>();
            try
            {
                // Keep the unreadable original, then reinstate the verified backup as main.
                if (files.Exists(MainPath)) { var target = Aside(MainPath, "unreadable"); files.Move(MainPath, target); preserved.Add(target); }
                files.WriteAllBytes(TempPath, backup.Bytes);
                files.Move(TempPath, MainPath);
            }
            catch (Exception e) when (IsStorage(e))
            {
                return new SaveLoadResult(SaveLoadStatus.Unreadable, null, string.Join(" ", problems) + " Backup recovery failed: " + e.Message, preserved);
            }
            Adopt(backup);
            return new SaveLoadResult(SaveLoadStatus.RecoveredFromBackup, backup.Snapshot,
                "Recovered the previous save. " + string.Join(" ", problems), preserved, backup.Notice, backup.Abandoned);
        }

        private sealed class ReadResult
        {
            public SessionSnapshot Snapshot;
            public byte[] Bytes;
            public Guid Request;
            public string Notice;
            public ExpeditionState Abandoned;
        }

        private void Adopt(ReadResult loaded)
        {
            ready = true; committedRevision = loaded.Snapshot.Revision; committedRequest = loaded.Request;
        }

        private ReadResult TryRead(string path, List<string> problems)
        {
            string name = Path.GetFileName(path);
            try
            {
                if (!files.Exists(path)) { problems.Add(name + " is missing."); return null; }
                var bytes = files.ReadAllBytes(path);
                var dto = SaveCodec.Decode(bytes);
                var snapshot = SaveMapper.FromDto(dto);
                // Checked before validation: a voyage from an older world may name
                // regions or definitions this catalog no longer has.
                string notice = VoyageProblem(snapshot.Expedition);
                var abandoned = notice.Length > 0 ? snapshot.Expedition : null;
                if (abandoned != null) snapshot = SaveMigration.AbandonVoyage(snapshot);
                var valid = CampaignSession.ValidateSnapshot(definitions, snapshot);
                if (!valid.IsSuccess) { problems.Add(name + ": " + valid.Error + " " + valid.Detail); return null; }
                var request = dto.request == null ? Guid.Empty : Guid.ParseExact(dto.request, "D");
                return new ReadResult { Snapshot = snapshot, Bytes = bytes, Request = request, Notice = notice, Abandoned = abandoned };
            }
            catch (SaveFormatException e) { problems.Add(name + ": " + e.Message); return null; }
            catch (FormatException e) { problems.Add(name + ": " + e.Message); return null; }
            catch (Exception e) when (IsStorage(e)) { problems.Add(name + ": " + e.Message); return null; }
        }

        // A failing check counts as a problem: losing one voyage's cargo is better
        // than a campaign that cannot be loaded at all.
        private string VoyageProblem(ExpeditionState voyage)
        {
            if (voyage == null || voyageProblem == null) return "";
            try { return voyageProblem(voyage) ?? ""; }
            catch (Exception e) when (!(e is OutOfMemoryException)) { return "The voyage could not be checked: " + e.Message; }
        }

        private RuleResult Write(SessionSnapshot snapshot, Guid request, string command)
        {
            try
            {
                var bytes = SaveCodec.Encode(SaveMapper.ToDto(snapshot, request, command, clock()));
                files.CreateDirectory(Directory);
                files.WriteAllBytes(TempPath, bytes);
                // Read back before replacement: a failed or torn write never becomes the main save.
                var check = SaveMapper.FromDto(SaveCodec.Decode(files.ReadAllBytes(TempPath)));
                if (check.Revision != snapshot.Revision) throw new SaveFormatException("Written save did not verify.");
                if (files.Exists(MainPath)) files.Replace(TempPath, MainPath, BackupPath);
                else files.Move(TempPath, MainPath);
                return new RuleResult();
            }
            catch (SaveFormatException e) { return Failed(e.Message); }
            catch (Exception e) when (IsStorage(e)) { return Failed(e.Message); }
        }

        private string Aside(string path, string reason)
        {
            string stamp = clock().ToUniversalTime().ToString("yyyyMMdd'T'HHmmss'Z'", CultureInfo.InvariantCulture);
            string stem = Path.GetFileNameWithoutExtension(path);
            for (int i = 0; ; i++)
            {
                string candidate = Path.Combine(Directory, stem + "." + reason + "-" + stamp + (i == 0 ? "" : "-" + i) + ".json");
                if (!files.Exists(candidate)) return candidate;
            }
        }

        private static bool IsStorage(Exception e) =>
            e is IOException || e is UnauthorizedAccessException || e is NotSupportedException || e is System.Security.SecurityException;

        private static RuleResult Failed(string detail) => new RuleResult(RuleError.SaveFailed, detail: detail);
    }
}
