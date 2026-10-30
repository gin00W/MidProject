using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace SeongbukAR.Notes
{
    [Serializable]
    public sealed class PersonalWasteNote
    {
        public string id;
        public string name;
        public string material;
        public string disposalMemo;
        public string sourceUrl;
        public string updatedUtc;
    }

    [Serializable]
    public sealed class PersonalWasteNoteData
    {
        public int schemaVersion = 1;
        public PersonalWasteNote[] items = Array.Empty<PersonalWasteNote>();
    }

    // Personal records never become catalog rules or recognition training data.
    // Copies at the boundary prevent an unfinished edit mutating a saved note.
    public sealed class PersonalWasteNotes
    {
        public const int MaximumItems = 500;
        public const int MaximumNameLength = 60;
        public const int MaximumMaterialLength = 120;
        public const int MaximumMemoLength = 1000;
        public const int MaximumUrlLength = 500;

        readonly List<PersonalWasteNote> _items = new List<PersonalWasteNote>();

        public PersonalWasteNoteData Data => new PersonalWasteNoteData
        {
            schemaVersion = 1,
            items = _items.Select(Clone).ToArray()
        };

        public PersonalWasteNotes(PersonalWasteNoteData data)
        {
            if (data == null || data.schemaVersion != 1 || data.items == null)
                throw new ArgumentException("내 메모 파일의 형식을 확인해 주세요.");
            if (data.items.Length > MaximumItems)
                throw new ArgumentException("내 메모는 최대 500개까지 저장할 수 있습니다.");
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var item in data.items)
            {
                var copy = ValidatedCopy(item, false);
                if (!ids.Add(copy.id)) throw new ArgumentException("내 메모의 ID가 중복되어 있습니다.");
                _items.Add(copy);
            }
        }

        public PersonalWasteNote Find(string id)
        {
            return Clone(_items.Find(item => item.id == id));
        }

        public IEnumerable<PersonalWasteNote> Search(string query)
        {
            string needle = Normalize(query);
            return _items.Where(item => needle.Length == 0 ||
                    Normalize(item.name).Contains(needle) || Normalize(item.material).Contains(needle) ||
                    Normalize(item.disposalMemo).Contains(needle))
                .OrderByDescending(item => item.updatedUtc, StringComparer.Ordinal)
                .ThenBy(item => item.name, StringComparer.Ordinal)
                .Select(Clone).ToArray();
        }

        public PersonalWasteNote Upsert(PersonalWasteNote note)
        {
            var copy = ValidatedCopy(note, true);
            int index = _items.FindIndex(item => item.id == copy.id);
            if (index < 0 && _items.Count >= MaximumItems)
                throw new ArgumentException("내 메모는 최대 500개까지 저장할 수 있습니다.");
            copy.updatedUtc = DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture);
            if (index < 0) _items.Add(copy);
            else _items[index] = copy;
            return Clone(copy);
        }

        public bool Remove(string id)
        {
            int index = _items.FindIndex(item => item.id == id);
            if (index < 0) return false;
            _items.RemoveAt(index);
            return true;
        }

        static PersonalWasteNote ValidatedCopy(PersonalWasteNote note, bool editing)
        {
            if (note == null) throw new ArgumentException("저장할 메모가 없습니다.");
            string id = (note.id ?? "").Trim();
            if (editing && id.Length == 0) id = Guid.NewGuid().ToString("N");
            Guid parsedId;
            if (id.Length != 32 || !Guid.TryParseExact(id, "N", out parsedId))
                throw new ArgumentException("내 메모 ID의 형식을 확인해 주세요.");
            id = parsedId.ToString("N");
            string name = Clean(note.name, MaximumNameLength, "물건 이름");
            if (name.Length == 0) throw new ArgumentException("물건 이름을 입력해 주세요.");
            string material = Clean(note.material, MaximumMaterialLength, "재질 메모");
            string memo = Clean(note.disposalMemo, MaximumMemoLength, "배출 메모");
            string url = Clean(note.sourceUrl, MaximumUrlLength, "참고 주소");
            if (url.Length > 0)
            {
                Uri uri;
                if (!Uri.TryCreate(url, UriKind.Absolute, out uri) ||
                    uri.Scheme != Uri.UriSchemeHttps || string.IsNullOrWhiteSpace(uri.Host) ||
                    !string.IsNullOrEmpty(uri.UserInfo))
                    throw new ArgumentException("참고 주소는 https://로 시작하는 웹 주소를 입력해 주세요.");
            }
            string updated = (note.updatedUtc ?? "").Trim();
            if (!editing)
            {
                DateTimeOffset timestamp;
                if (!DateTimeOffset.TryParseExact(updated, "O", CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out timestamp) || timestamp.Offset != TimeSpan.Zero)
                    throw new ArgumentException("내 메모의 저장 날짜를 확인해 주세요.");
            }
            return new PersonalWasteNote
            {
                id = id, name = name, material = material,
                disposalMemo = memo, sourceUrl = url, updatedUtc = updated
            };
        }

        static string Clean(string value, int maximum, string label)
        {
            string clean = (value ?? "").Trim();
            if (clean.Length > maximum)
                throw new ArgumentException(label + "은 " + maximum + "자 이내로 입력해 주세요.");
            return clean;
        }

        static string Normalize(string value)
        {
            return string.Concat((value ?? "").Where(c => !char.IsWhiteSpace(c))).ToLowerInvariant();
        }

        static PersonalWasteNote Clone(PersonalWasteNote note)
        {
            return note == null ? null : new PersonalWasteNote
            {
                id = note.id, name = note.name, material = note.material,
                disposalMemo = note.disposalMemo, sourceUrl = note.sourceUrl, updatedUtc = note.updatedUtc
            };
        }
    }
}
