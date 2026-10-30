using System;
using System.IO;
using System.Text;

namespace SeongbukAR.Notes
{
    // The file transaction is independent of Unity so failure/recovery paths
    // can be exercised with the same production implementation on .NET.
    public sealed class PersonalWasteNoteFile
    {
        public const int MaximumFileBytes = 1024 * 1024;
        readonly string _path;
        readonly Func<string, PersonalWasteNoteData> _deserialize;
        readonly Func<PersonalWasteNoteData, string> _serialize;

        public PersonalWasteNoteFile(string path, Func<string, PersonalWasteNoteData> deserialize,
            Func<PersonalWasteNoteData, string> serialize)
        {
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("메모 저장 경로가 없습니다.");
            _path = Path.GetFullPath(path);
            _deserialize = deserialize ?? throw new ArgumentNullException(nameof(deserialize));
            _serialize = serialize ?? throw new ArgumentNullException(nameof(serialize));
        }

        public PersonalWasteNoteData Load()
        {
            if (File.Exists(_path)) return ReadValidated(_path);
            // Covers an interrupted replacement on platforms without File.Replace.
            if (File.Exists(_path + ".bak")) return ReadValidated(_path + ".bak");
            return new PersonalWasteNoteData();
        }

        public void Save(PersonalWasteNoteData data)
        {
            var clean = new PersonalWasteNotes(data).Data;
            string json = _serialize(clean);
            if (string.IsNullOrWhiteSpace(json)) throw new IOException("저장할 메모 파일을 만들지 못했습니다.");
            byte[] bytes = new UTF8Encoding(false).GetBytes(json);
            if (bytes.Length > MaximumFileBytes)
                throw new IOException("내 메모 저장 용량을 초과했습니다. 긴 메모를 줄인 뒤 다시 저장해 주세요.");
            // Never overwrite an unreadable/corrupt file with an empty/new set.
            if (File.Exists(_path)) ReadValidated(_path);
            Directory.CreateDirectory(Path.GetDirectoryName(_path));
            string temporary = _path + ".tmp";
            string backup = _path + ".bak";
            try
            {
                using (var stream = new FileStream(temporary, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    stream.Write(bytes, 0, bytes.Length);
                    stream.Flush(true);
                }
                if (!File.Exists(_path)) File.Move(temporary, _path);
                else
                {
                    try { File.Replace(temporary, _path, backup, true); }
                    catch (PlatformNotSupportedException) { ReplaceWithBackup(temporary, backup); }
                }
            }
            finally
            {
                // Cleanup must not conceal the original read/write exception.
                try { if (File.Exists(temporary)) File.Delete(temporary); }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
            }
        }

        PersonalWasteNoteData ReadValidated(string path)
        {
            if (new FileInfo(path).Length > MaximumFileBytes)
                throw new IOException("내 메모 파일이 허용된 크기를 초과했습니다.");
            string json = File.ReadAllText(path, Encoding.UTF8);
            if (string.IsNullOrWhiteSpace(json)) throw new IOException("내 메모 파일이 비어 있습니다.");
            return new PersonalWasteNotes(_deserialize(json)).Data;
        }

        void ReplaceWithBackup(string temporary, string backup)
        {
            File.Copy(_path, backup, true);
            File.Delete(_path);
            try { File.Move(temporary, _path); }
            catch
            {
                // If restoration itself fails, the intact .bak is loaded next time.
                if (!File.Exists(_path)) File.Copy(backup, _path);
                throw;
            }
        }
    }
}
