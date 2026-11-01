using System.IO;
using UnityEngine;

namespace SeongbukAR.Notes
{
    // Device-local notes: no network calls, shared database or classifier edits.
    public static class PersonalWasteNoteStore
    {
        public const string FileName = "personal-waste-notes.json";

        public static PersonalWasteNoteData Load() => CreateFile().Load();
        public static void Save(PersonalWasteNoteData data) => CreateFile().Save(data);

        static PersonalWasteNoteFile CreateFile()
        {
            return new PersonalWasteNoteFile(Path.Combine(Application.persistentDataPath, FileName),
                Deserialize, data => JsonUtility.ToJson(data, true));
        }

        static PersonalWasteNoteData Deserialize(string json)
        {
            // Sentinel fields make a missing envelope fail even on Unity versions
            // which run serializable field initializers during deserialization.
            var data = new PersonalWasteNoteData { schemaVersion = 0, items = null };
            JsonUtility.FromJsonOverwrite(json, data);
            return data;
        }
    }
}
