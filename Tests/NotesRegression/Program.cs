using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using SeongbukAR.Notes;

static class Program
{
    static int _passed;
    static int _failed;
    static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions { IncludeFields = true };

    static int Main()
    {
        Run("new unknown item can be saved with only its name", () =>
        {
            var notes = Empty();
            var saved = notes.Upsert(new PersonalWasteNote { name = "  실리콘 빨대  " });
            Check(saved.name == "실리콘 빨대" && saved.id.Length == 32, "Name/identity missing.");
            Check(saved.disposalMemo == "" && saved.sourceUrl == "", "Unverified data was invented.");
            Check(new PersonalWasteNotes(notes.Data).Find(saved.id).updatedUtc == saved.updatedUtc,
                "Saved UTC timestamp did not survive validation.");
        });
        Run("editing and deleting notes preserve item identity", () =>
        {
            var notes = Empty();
            var saved = notes.Upsert(Note("볼펜 부품"));
            saved.disposalMemo = "구청에 재질을 확인할 예정";
            notes.Upsert(saved);
            Check(notes.Data.items.Length == 1 && notes.Find(saved.id).disposalMemo == saved.disposalMemo,
                "Editing duplicated or dropped a note.");
            Check(notes.Remove(saved.id) && !notes.Remove(saved.id) && notes.Find(saved.id) == null,
                "Deletion left a searchable record.");
        });
        Run("search covers name material and memo with whitespace normalization", () =>
        {
            var notes = Empty();
            notes.Upsert(new PersonalWasteNote
                { name = "작은 고리", material = "스테인리스", disposalMemo = "SUS 재질 구청 확인" });
            Check(notes.Search("작 은고리").Count() == 1, "Spaced Korean name was not found.");
            Check(notes.Search("스테 인리스").Count() == 1 && notes.Search("sus").Count() == 1,
                "Material/memo search failed.");
            Check(!notes.Search("다른 물건").Any(), "Unknown query produced a fabricated match.");
        });
        Run("invalid edit does not mutate existing note or the input draft", () =>
        {
            var notes = Empty();
            var saved = notes.Upsert(Note("우산 손잡이"));
            var draft = notes.Find(saved.id);
            draft.name = " ";
            Reject(() => notes.Upsert(draft));
            Check(notes.Find(saved.id).name == "우산 손잡이" && draft.name == " ", "Rejected edit changed data.");
        });
        Run("returned snapshots cannot modify stored data", () =>
        {
            var notes = Empty();
            var draft = Note("실리콘 덮개");
            var saved = notes.Upsert(draft);
            draft.name = "draft changed";
            saved.name = "return changed";
            var data = notes.Data;
            data.items[0].name = "snapshot changed";
            var search = notes.Search("").First();
            search.name = "search changed";
            Check(notes.Search("").Single().name == "실리콘 덮개", "A caller mutated stored data by reference.");
        });
        Run("note schema duplicate IDs dates and item limit are validated", () =>
        {
            Reject(() => new PersonalWasteNotes(new PersonalWasteNoteData { schemaVersion = 2 }));
            Reject(() => new PersonalWasteNotes(new PersonalWasteNoteData { items = null }));
            var notes = Empty();
            var saved = notes.Upsert(Note("고무 부품"));
            Reject(() => new PersonalWasteNotes(new PersonalWasteNoteData { items = new[] { saved, saved } }));
            saved.updatedUtc = "today";
            Reject(() => new PersonalWasteNotes(new PersonalWasteNoteData { items = new[] { saved } }));
            Reject(() => new PersonalWasteNotes(new PersonalWasteNoteData
                { items = new PersonalWasteNote[PersonalWasteNotes.MaximumItems + 1] }));
        });
        Run("length limits reject oversized values without truncating user data", () =>
        {
            var notes = Empty();
            Reject(() => notes.Upsert(new PersonalWasteNote { name = new string('가', 61) }));
            Reject(() => notes.Upsert(new PersonalWasteNote { name = "물건", material = new string('가', 121) }));
            Reject(() => notes.Upsert(new PersonalWasteNote { name = "물건", disposalMemo = new string('가', 1001) }));
            Reject(() => notes.Upsert(new PersonalWasteNote { name = "물건", sourceUrl = new string('a', 501) }));
            Check(notes.Data.items.Length == 0, "Oversized data was partially saved.");
        });
        Run("references accept only absolute HTTPS links without credentials", () =>
        {
            var notes = Empty();
            foreach (string invalid in new[] { "file:///tmp/private", "javascript:alert(1)", "http://example.com",
                "https://user:password@example.com", "품목사전" })
                Reject(() => notes.Upsert(new PersonalWasteNote { name = "물건", sourceUrl = invalid }));
            var saved = notes.Upsert(new PersonalWasteNote
                { name = "물건", sourceUrl = "https://www.wasteguide.or.kr/front/dischargeMethod/dictionary.do" });
            Check(saved.sourceUrl.StartsWith("https://"), "Official HTTPS reference was dropped.");
        });
        WithFiles("notes survive a save and new process-style reload", path =>
        {
            var notes = Empty();
            notes.Upsert(new PersonalWasteNote
                { name = "실리콘 빨대", material = "실리콘", disposalMemo = "확인할 내용\n아직 배출 방법은 모름" });
            FileAt(path).Save(notes.Data);
            var loaded = new PersonalWasteNotes(FileAt(path).Load());
            Check(loaded.Search("빨대").Single().disposalMemo.Contains("\n"), "Unicode/newline record did not round trip.");
        });
        WithFiles("replacement keeps the previous valid version as a backup", path =>
        {
            var notes = Empty();
            var saved = notes.Upsert(Note("이전 이름"));
            FileAt(path).Save(notes.Data);
            saved.name = "수정한 이름";
            notes.Upsert(saved);
            FileAt(path).Save(notes.Data);
            Check(new PersonalWasteNotes(FileAt(path).Load()).Search("수정한 이름").Count() == 1,
                "Replacement did not save new record.");
            Check(new PersonalWasteNotes(FileAt(path + ".bak").Load()).Search("이전 이름").Count() == 1,
                "Previous version was not backed up.");
            Check(!File.Exists(path + ".tmp"), "Completed transaction left a temporary file.");
        });
        WithFiles("missing primary recovers the intact backup after interrupted replacement", path =>
        {
            var notes = Empty();
            notes.Upsert(Note("백업 물건"));
            FileAt(path + ".bak").Save(notes.Data);
            Check(new PersonalWasteNotes(FileAt(path).Load()).Search("백업 물건").Count() == 1,
                "Backup recovery returned an empty notebook.");
        });
        WithFiles("corrupted primary is reported and never overwritten with a new notebook", path =>
        {
            const string broken = "{not valid json";
            File.WriteAllText(path, broken);
            Reject(() => FileAt(path).Load());
            Reject(() => FileAt(path).Save(Empty().Data));
            Check(File.ReadAllText(path) == broken, "Corrupt source was silently replaced.");
        });
        WithFiles("oversized file is rejected before parsing", path =>
        {
            File.WriteAllText(path, new string('x', PersonalWasteNoteFile.MaximumFileBytes + 1));
            Reject(() => FileAt(path).Load());
            Check(new FileInfo(path).Length > PersonalWasteNoteFile.MaximumFileBytes, "Oversized source was changed.");
        });
        WithFiles("serializer failure leaves previous records intact", path =>
        {
            var notes = Empty();
            notes.Upsert(Note("기존 메모"));
            FileAt(path).Save(notes.Data);
            string original = File.ReadAllText(path);
            var failing = new PersonalWasteNoteFile(path, Deserialize,
                data => throw new IOException("Simulated serialization failure."));
            Reject(() => failing.Save(Empty().Data));
            Check(File.ReadAllText(path) == original, "Failure destroyed the previous file.");
        });
        WithFiles("write failure surfaces and leaves original destination intact", path =>
        {
            var notes = Empty();
            notes.Upsert(Note("보존할 메모"));
            FileAt(path).Save(notes.Data);
            string original = File.ReadAllText(path);
            Directory.CreateDirectory(path + ".tmp");
            Reject(() => FileAt(path).Save(Empty().Data));
            Check(File.ReadAllText(path) == original, "Failed write modified destination.");
        });
        Console.WriteLine("Passed " + _passed + " personal-note checks; failed " + _failed + ".");
        return _failed == 0 ? 0 : 1;
    }

    static PersonalWasteNotes Empty() => new PersonalWasteNotes(new PersonalWasteNoteData());
    static PersonalWasteNote Note(string name) => new PersonalWasteNote { name = name };
    static PersonalWasteNoteFile FileAt(string path) => new PersonalWasteNoteFile(path, Deserialize,
        data => JsonSerializer.Serialize(data, JsonOptions));
    static PersonalWasteNoteData Deserialize(string json)
    {
        using (var document = JsonDocument.Parse(json))
        {
            if (!document.RootElement.TryGetProperty("schemaVersion", out _) ||
                !document.RootElement.TryGetProperty("items", out _))
                throw new ArgumentException("Missing envelope.");
        }
        return JsonSerializer.Deserialize<PersonalWasteNoteData>(json, JsonOptions);
    }

    static void WithFiles(string label, Action<string> test)
    {
        Run(label, () =>
        {
            string directory = Path.Combine(Path.GetTempPath(), "seongbuk-notes-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            try { test(Path.Combine(directory, "notes.json")); }
            finally { Directory.Delete(directory, true); }
        });
    }
    static void Reject(Action action)
    {
        try { action(); }
        catch (ArgumentException) { return; }
        catch (IOException) { return; }
        catch (UnauthorizedAccessException) { return; }
        catch (JsonException) { return; }
        throw new Exception("Invalid input or failed operation was accepted.");
    }
    static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }
    static void Run(string label, Action test)
    {
        try { test(); _passed++; Console.WriteLine("PASS " + label); }
        catch (Exception error) { _failed++; Console.WriteLine("FAIL " + label + ": " + error.Message); }
    }
}
