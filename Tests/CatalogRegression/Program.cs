using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using SeongbukAR;

// WasteCatalog has no Unity dependency. These checks exercise the production
// C# validator with the empty result objects observed in Unity's serializer.
// They do not substitute for running JsonUtility and the UI in Unity itself.
static class Program
{
    static string _json;
    static int _passed;

    static int Main(string[] args)
    {
        string dataPath = args.Length > 0 ? args[0] :
            Path.Combine("Assets", "SeongbukAR", "Resources", "Data", "seongbuk-demo.json");
        _json = File.ReadAllText(dataPath);

        Run("sample catalog loads", () =>
        {
            var catalog = new WasteCatalog(Load());
            Check(catalog.Data.items.Length == 6, "Expected six sample items.");
        });
        Run("Unity-created empty results do not turn questions into results", () =>
        {
            var catalog = new WasteCatalog(LoadWithEmptyResults());
            Check(catalog.Find("pet_bottle").FindNode("contents").choices.Length == 2,
                "PET question choices must remain available.");
            Check(catalog.Find("plastic_container").FindNode("can_clean").choices.Length == 3,
                "Container condition choices must remain available.");
        });
        Run("empty strings and arrays also represent an absent result", () =>
        {
            var data = LoadWithEmptyResults();
            data.items[0].nodes[0].result = new GuideResult
            {
                destination = " ", explanation = "", steps = Array.Empty<string>(),
                sourceUrls = Array.Empty<string>()
            };
            new WasteCatalog(data);
        });
        Run("question and authored result together are rejected", () =>
        {
            var data = Load();
            data.items[0].nodes[0].result = data.items[0].nodes[1].result;
            Reject(data);
        });
        Run("node with neither question nor result is rejected", () =>
        {
            var data = LoadWithEmptyResults();
            data.items[0].nodes[0].choices = Array.Empty<GuideChoice>();
            Reject(data);
        });
        Run("incomplete authored result is still rejected", () =>
        {
            var data = Load();
            data.items[0].nodes[1].result.destination = "";
            Reject(data);
        });
        Run("authored result without steps is rejected", () =>
        {
            var data = Load();
            data.items[0].nodes[1].result.steps = Array.Empty<string>();
            Reject(data);
        });
        Run("authored result without sources is rejected", () =>
        {
            var data = Load();
            data.items[0].nodes[1].result.sourceUrls = Array.Empty<string>();
            Reject(data);
        });
        Run("missing choice destination is rejected", () =>
        {
            var data = Load();
            data.items[0].nodes[0].choices[0].nextNodeId = "missing";
            Reject(data);
        });
        Run("aliases and whitespace searches work after loading", () =>
        {
            var catalog = new WasteCatalog(LoadWithEmptyResults());
            Check(catalog.Search("엽떡").Single().id == "plastic_container", "Alias lookup failed.");
            Check(catalog.Search("치킨뼈").Single().id == "chicken_bone", "Whitespace lookup failed.");
            Check(!catalog.Search("not-a-registered-item").Any(), "Unknown item must not match.");
        });
        Run("all sample answer paths reach complete results", () =>
        {
            var catalog = new WasteCatalog(LoadWithEmptyResults());
            foreach (var item in catalog.Data.items) Walk(item, item.startNodeId, 0);
        });
        Console.WriteLine("Passed " + _passed + " catalog regression checks.");
        return 0;
    }

    static WasteCatalogData Load()
    {
        return JsonSerializer.Deserialize<WasteCatalogData>(_json,
            new JsonSerializerOptions { IncludeFields = true });
    }

    static WasteCatalogData LoadWithEmptyResults()
    {
        var data = Load();
        foreach (var node in data.items.SelectMany(item => item.nodes))
        {
            if (node.result == null) node.result = new GuideResult();
            if (node.choices == null) node.choices = Array.Empty<GuideChoice>();
        }
        return data;
    }

    static void Walk(WasteItem item, string id, int depth)
    {
        Check(depth <= item.nodes.Length, "Sample question graph contains a cycle.");
        var node = item.FindNode(id);
        if (node.choices.Length == 0)
        {
            Check(!string.IsNullOrWhiteSpace(node.result.destination), "Missing result destination.");
            Check(node.result.steps.Length > 0 && node.result.sourceUrls.Length > 0, "Incomplete result.");
            return;
        }
        foreach (var choice in node.choices) Walk(item, choice.nextNodeId, depth + 1);
    }

    static void Reject(WasteCatalogData data)
    {
        try { new WasteCatalog(data); }
        catch (ArgumentException) { return; }
        throw new Exception("Malformed catalog was accepted.");
    }

    static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }

    static void Run(string label, Action test)
    {
        test();
        _passed++;
        Console.WriteLine("PASS " + label);
    }
}
