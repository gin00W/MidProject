using System;
using System.Collections.Generic;
using System.Linq;

namespace SeongbukAR
{
    [Serializable]
    public sealed class WasteCatalogData
    {
        public int schemaVersion;
        public string region;
        public WasteItem[] items;
    }

    [Serializable]
    public sealed class WasteItem
    {
        public string id;
        public string name;
        public string[] aliases;
        public string reviewStatus;
        public string startNodeId;
        public GuideNode[] nodes;

        public GuideNode FindNode(string nodeId)
        {
            return Array.Find(nodes, node => node.id == nodeId);
        }
    }

    [Serializable]
    public sealed class GuideNode
    {
        public string id;
        public string prompt;
        public GuideChoice[] choices;
        public GuideResult result;

        // Unity's inline serializer may materialize a missing result as an
        // empty object. Use its content, not object presence, to classify it.
        public bool IsResult => result != null && result.HasContent;
    }

    [Serializable]
    public sealed class GuideChoice
    {
        public string label;
        public string nextNodeId;
    }

    [Serializable]
    public sealed class GuideResult
    {
        public string destination;
        public string[] steps;
        public string explanation;
        public string[] sourceUrls;

        public bool HasContent =>
            !string.IsNullOrWhiteSpace(destination) ||
            (steps != null && steps.Length > 0) ||
            !string.IsNullOrWhiteSpace(explanation) ||
            (sourceUrls != null && sourceUrls.Length > 0);
    }

    // The same data drives both the Editor preview and the iPhone AR scene.
    // Add items/questions in JSON rather than creating a script per item.
    public sealed class WasteCatalog
    {
        public WasteCatalogData Data { get; }

        public WasteCatalog(WasteCatalogData data)
        {
            if (data == null || data.schemaVersion != 1 || data.items == null)
                throw new ArgumentException("품목 데이터의 형식을 확인해 주세요.");

            var ids = new HashSet<string>();
            foreach (var item in data.items)
            {
                if (item == null || string.IsNullOrWhiteSpace(item.id) ||
                    string.IsNullOrWhiteSpace(item.name) || !ids.Add(item.id) ||
                    item.nodes == null || item.nodes.Length == 0)
                    throw new ArgumentException("품목 ID, 이름, 질문 목록을 확인해 주세요.");

                var nodeIds = new HashSet<string>();
                foreach (var node in item.nodes)
                {
                    if (node == null || string.IsNullOrWhiteSpace(node.id) || !nodeIds.Add(node.id))
                        throw new ArgumentException(item.id + ": 질문 ID가 비었거나 중복입니다.");

                    bool hasChoices = node.choices != null && node.choices.Length > 0;
                    bool hasResult = node.IsResult;
                    if (hasChoices == hasResult)
                        throw new ArgumentException(item.id + "/" + node.id + ": 질문 또는 결과 중 하나가 필요합니다.");
                    if (hasChoices && string.IsNullOrWhiteSpace(node.prompt))
                        throw new ArgumentException(item.id + ": 질문 문장이 필요합니다.");
                    if (hasResult &&
                        (string.IsNullOrWhiteSpace(node.result.destination) ||
                         node.result.steps == null || node.result.steps.Length == 0 ||
                         node.result.sourceUrls == null || node.result.sourceUrls.Length == 0))
                        throw new ArgumentException(item.id + ": 배출 방법과 출처를 확인해 주세요.");
                }
                if (!nodeIds.Contains(item.startNodeId ?? ""))
                    throw new ArgumentException(item.id + ": 시작 질문을 찾을 수 없습니다.");
                foreach (var node in item.nodes)
                    foreach (var choice in node.choices ?? Array.Empty<GuideChoice>())
                        if (choice == null || string.IsNullOrWhiteSpace(choice.label) ||
                            !nodeIds.Contains(choice.nextNodeId ?? ""))
                            throw new ArgumentException(item.id + ": 선택지의 연결을 확인해 주세요.");
            }
            Data = data;
        }

        public WasteItem Find(string id) => Array.Find(Data.items, item => item.id == id);

        public IEnumerable<WasteItem> Search(string query)
        {
            string needle = Normalize(query);
            return Data.items.Where(item => needle.Length == 0 ||
                Normalize(item.name).Contains(needle) ||
                (item.aliases ?? Array.Empty<string>()).Any(alias => Normalize(alias).Contains(needle)));
        }

        static string Normalize(string value)
        {
            return string.Concat((value ?? "").Where(c => !char.IsWhiteSpace(c))).ToLowerInvariant();
        }
    }
}
