using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
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
        public SaveLoadResult(SaveLoadStatus status, SessionSnapshot snapshot, string detail, IReadOnlyList<string> preserved)
        { Status = status; Snapshot = snapshot; Detail = detail ?? ""; PreservedFiles = preserved ?? Array.Empty<string>(); }
        public bool HasCampaign => Snapshot != null;
    }

    // Single ordered writer for one campaign. Every commit writes a same-volume
    // temporary file, reads it back, then atomically replaces the main save and
    // keeps the previous committed snapshot as the backup. Unreadable or
    // unsupported files are preserved under new names, never overwritten.
    //
    // With background writes on, the frequent at-sea saves (checkpoints and
    // pickups) are acknowledged once queued and written in order on a worker
    // thread, so they no longer stall a frame. Every other commit, load and
    // initialization first waits for the queue, so an older snapshot can never
    // land after a newer one. A crash can lose only the saves still queued,
    // which INV-17 allows for checkpoints. After a failed background write the
    // next commit runs synchronously, so the failure reaches the normal
    // SaveFailed/retry path.
    public sealed class JsonSaveStore : ISaveStore
    {
        public const string MainName = "campaign.json";
        public const string BackupName = "campaign.backup.json";
        public const string TempName = "campaign.json.tmp";

        private static readonly HashSet<string> BackgroundCommands = new HashSet<string>(StringComparer.Ordinal) { "Checkpoint", "CollectLoot" };
        // One queue per save folder, shared by every store in the process, so a
        // store created by the next scene still sees the previous scene's writes.
        private static readonly object QueueLock = new object();
        private static readonly Dictionary<string, Task> Queues = new Dictionary<string, Task>(StringComparer.OrdinalIgnoreCase);

        private readonly ISaveFileSystem files;
        private readonly DefinitionCatalog definitions;
        private readonly Func<DateTime> clock;
        private bool ready;
        private long committedRevision;
        private Guid committedRequest;
        private volatile string backgroundFailure;

        public string Directory { get; }
        public string MainPath => Path.Combine(Directory, MainName);
        public string BackupPath => Path.Combine(Directory, BackupName);
        public string TempPath => Path.Combine(Directory, TempName);
        public bool BackgroundWrites { get; }
        public bool HasSaveFiles { get { Flush(); return files.Exists(MainPath) || files.Exists(BackupPath); } }
        public long? CommittedRevision => ready ? committedRevision : (long?)null;

        public JsonSaveStore(string directory, DefinitionCatalog definitions, ISaveFileSystem files = null, Func<DateTime> clock = null,
            bool backgroundWrites = false)
        {
            if (string.IsNullOrWhiteSpace(directory)) throw new ArgumentException("A save directory is required.");
            Directory = Path.GetFullPath(directory);
            this.definitions = definitions ?? throw new ArgumentNullException(nameof(definitions));
            this.files = files ?? new DiskFileSystem();
            this.clock = clock ?? (() => DateTime.UtcNow);
            BackgroundWrites = backgroundWrites;
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
            if (BackgroundWrites && BackgroundCommands.Contains(candidate.Command) && backgroundFailure == null)
            {
                // Snapshots are immutable, so the worker can encode and write this one later.
                var snapshot = candidate.Snapshot; var request = candidate.RequestId; var command = candidate.Command;
                Enqueue(() => backgroundFailure = WriteSafely(snapshot, request, command));
                committedRevision = revision; committedRequest = request;
                return new RuleResult();
            }
            Flush();
            var written = Write(candidate.Snapshot, candidate.RequestId, candidate.Command);
            if (!written.IsSuccess) return written;
            backgroundFailure = null;
            committedRevision = revision; committedRequest = candidate.RequestId;
            return written;
        }

        // Blocks until every queued write for this save folder has finished.
        public void Flush() => Flush(Directory);

        public static void Flush(string directory)
        {
            Task queue;
            lock (QueueLock) Queues.TryGetValue(Path.GetFullPath(directory), out queue);
            queue?.Wait();
        }

        private void Enqueue(Action write)
        {
            lock (QueueLock)
            {
                Queues.TryGetValue(Directory, out var queue);
                Queues[Directory] = (queue ?? Task.CompletedTask).ContinueWith(_ => write(), CancellationToken.None,
                    TaskContinuationOptions.None, TaskScheduler.Default);
            }
        }

        // Worker-thread write: returns null on success or the failure detail.
        private string WriteSafely(SessionSnapshot snapshot, Guid request, string command)
        {
            try
            {
                var written = Write(snapshot, request, command);
                return written.IsSuccess ? null : written.Detail;
            }
            catch (Exception e) { return e.Message; }
        }

        // Creates a new campaign file. Existing files are renamed aside first.
        public RuleResult Initialize(SessionSnapshot initial, out IReadOnlyList<string> preserved)
        {
            var moved = new List<string>(); preserved = moved;
            if (initial == null) return Failed("Missing initial snapshot.");
            var valid = CampaignSession.ValidateSnapshot(definitions, initial);
            if (!valid.IsSuccess) return valid;
            Flush();
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
            Flush();
            if (!HasSaveFiles) return new SaveLoadResult(SaveLoadStatus.NoSave, null, "No saved campaign.", null);
            var problems = new List<string>();
            var main = TryRead(MainPath, problems);
            if (main != null) { Adopt(main); return new SaveLoadResult(SaveLoadStatus.Loaded, main.Item1, "", null); }
            var backup = TryRead(BackupPath, problems);
            if (backup == null)
                return new SaveLoadResult(SaveLoadStatus.Unreadable, null, string.Join(" ", problems), null);
            var preserved = new List<string>();
            try
            {
                // Keep the unreadable original, then reinstate the verified backup as main.
                if (files.Exists(MainPath)) { var target = Aside(MainPath, "unreadable"); files.Move(MainPath, target); preserved.Add(target); }
                files.WriteAllBytes(TempPath, backup.Item2);
                files.Move(TempPath, MainPath);
            }
            catch (Exception e) when (IsStorage(e))
            {
                return new SaveLoadResult(SaveLoadStatus.Unreadable, null, string.Join(" ", problems) + " Backup recovery failed: " + e.Message, preserved);
            }
            Adopt(backup);
            return new SaveLoadResult(SaveLoadStatus.RecoveredFromBackup, backup.Item1,
                "Recovered the previous save. " + string.Join(" ", problems), preserved);
        }

        private void Adopt(Tuple<SessionSnapshot, byte[], Guid> loaded)
        {
            ready = true; committedRevision = loaded.Item1.Revision; committedRequest = loaded.Item3;
        }

        private Tuple<SessionSnapshot, byte[], Guid> TryRead(string path, List<string> problems)
        {
            string name = Path.GetFileName(path);
            try
            {
                if (!files.Exists(path)) { problems.Add(name + " is missing."); return null; }
                var bytes = files.ReadAllBytes(path);
                var dto = SaveCodec.Decode(bytes);
                var snapshot = SaveMigration.AddNewHubs(definitions, SaveMapper.FromDto(dto));
                var valid = CampaignSession.ValidateSnapshot(definitions, snapshot);
                if (!valid.IsSuccess) { problems.Add(name + ": " + valid.Error + " " + valid.Detail); return null; }
                var request = dto.request == null ? Guid.Empty : Guid.ParseExact(dto.request, "D");
                return Tuple.Create(snapshot, bytes, request);
            }
            catch (SaveFormatException e) { problems.Add(name + ": " + e.Message); return null; }
            catch (FormatException e) { problems.Add(name + ": " + e.Message); return null; }
            catch (Exception e) when (IsStorage(e)) { problems.Add(name + ": " + e.Message); return null; }
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
