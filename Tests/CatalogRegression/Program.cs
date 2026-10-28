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
    static int _failed;

    static int Main(string[] args)
    {
        string dataPath = args.Length > 0 ? args[0] :
            Path.Combine("Assets", "SeongbukAR", "Resources", "Data", "seongbuk-demo.json");
        _json = File.ReadAllText(dataPath);

        Run("sample catalog loads", () =>
        {
            var catalog = new WasteCatalog(Load());
            Check(catalog.Data.items.Length >= 40, "Expected an expanded everyday-item catalog.");
            foreach (string id in new[] { "pet_bottle", "plastic_container", "chicken_bone", "peach_pit", "clothing", "small_appliance" })
                Check(catalog.Find(id) != null, "Existing item ID was removed: " + id);
        });
        Run("Unity-created empty results do not turn questions into results", () =>
        {
            var catalog = new WasteCatalog(LoadWithEmptyResults());
            Check(catalog.Find("pet_bottle").FindNode("contents").choices.Length >= 2,
                "PET question choices must remain available.");
            Check(catalog.Find("plastic_container").FindNode("can_clean").choices.Length >= 2,
                "Container condition choices must remain available.");
        });
        Run("empty strings and arrays also represent an absent result", () =>
        {
            var data = LoadWithEmptyResults();
            Node(data, "pet_bottle", "contents").result = new GuideResult
            {
                destination = " ", explanation = "", steps = Array.Empty<string>(),
                sourceUrls = Array.Empty<string>()
            };
            new WasteCatalog(data);
        });
        Run("question and authored result together are rejected", () =>
        {
            var data = Load();
            Node(data, "pet_bottle", "contents").result = Node(data, "pet_bottle", "clear_pet").result;
            Reject(data);
        });
        Run("node with neither question nor result is rejected", () =>
        {
            var data = LoadWithEmptyResults();
            Node(data, "pet_bottle", "contents").choices = Array.Empty<GuideChoice>();
            Reject(data);
        });
        Run("incomplete authored result is still rejected", () =>
        {
            var data = Load();
            Node(data, "pet_bottle", "clear_pet").result.destination = "";
            Reject(data);
        });
        Run("authored result without steps is rejected", () =>
        {
            var data = Load();
            Node(data, "pet_bottle", "clear_pet").result.steps = Array.Empty<string>();
            Reject(data);
        });
        Run("authored result without sources is rejected", () =>
        {
            var data = Load();
            Node(data, "pet_bottle", "clear_pet").result.sourceUrls = Array.Empty<string>();
            Reject(data);
        });
        Run("missing choice destination is rejected", () =>
        {
            var data = Load();
            Node(data, "pet_bottle", "contents").choices[0].nextNodeId = "missing";
            Reject(data);
        });
        Run("aliases and whitespace searches work after loading", () =>
        {
            var catalog = new WasteCatalog(LoadWithEmptyResults());
            Check(catalog.Search("엽떡").Single().id == "plastic_container", "Alias lookup failed.");
            Check(catalog.Search("치킨뼈").Single().id == "chicken_bone", "Whitespace lookup failed.");
            Check(!catalog.Search("not-a-registered-item").Any(), "Unknown item must not match.");
        });
        Run("expanded catalog exposes practical can and button guides", () =>
        {
            var catalog = new WasteCatalog(Load());
            foreach (string id in new[] { "beverage_can", "food_can", "button", "pet_cap", "receipt" })
                Check(catalog.Find(id) != null, "Requested everyday guide missing: " + id);
            Check(catalog.Search("단추").Any(item => item.id == "button"), "Korean button search failed.");
        });
        Run("all sample answer paths reach complete results", () =>
        {
            var catalog = new WasteCatalog(LoadWithEmptyResults());
            foreach (var item in catalog.Data.items) Walk(item, item.startNodeId, 0);
        });
        Run("explicit can label suggests confirmable beverage and food cans", () =>
        {
            var matches = WasteRecognition.Match(MatchingCatalog(), Signals("tin_can", .9f));
            Check(matches.Length == 2, "A generic can should preserve named alternatives.");
            Check(matches[0].item.id == "beverage_can" && matches[1].item.id == "food_can",
                "Expected beverage and food can suggestions.");
            Check(matches[0].confidence == .9f, "Must retain the original classifier score.");
        });
        Run("beverage can aliases reach the can guide without changing image scores", () =>
        {
            foreach (string label in new[] { "cola_can", "beer-cans", "soft_drink_can", "pop can",
                "ALUMINIUM BEVERAGE CAN", "pull-tab can", "tincan", "metal cans" })
            {
                var matches = WasteRecognition.Match(MatchingCatalog(), Signals(label, .61f));
                Check(matches.Length == 2 && matches[0].item.id == "beverage_can" &&
                    matches[1].item.id == "food_can", "Can alias did not preserve alternatives: " + label);
                Check(matches.All(candidate => candidate.confidence == .61f),
                    "Can alias changed the classifier confidence: " + label);
            }
        });
        Run("cardboard terminology reaches the box guide rather than ordinary paper", () =>
        {
            foreach (string label in new[] { "cardboard", "corrugated_cardboard", "corrugated carton",
                "shipping_carton", "cardboard boxes", "cardboard packaging" })
            {
                var matches = WasteRecognition.Match(MatchingCatalog(), Signals(label, .72f));
                Check(matches.Length == 1 && matches[0].item.id == "cardboard_box",
                    "Cardboard alias lost the specific box guide: " + label);
            }
        });
        Run("box shape keeps both cardboard and plastic possibilities", () =>
        {
            var matches = WasteRecognition.Match(MatchingCatalog(), Signals("box", .85f));
            Check(matches.Select(candidate => candidate.item.id).SequenceEqual(
                new[] { "cardboard_box", "plastic_container" }),
                "A box shape alone must not establish its material.");
        });
        Run("ambiguous carton includes a shipping box before drink cartons", () =>
        {
            var matches = WasteRecognition.Match(MatchingCatalog(), Signals("carton", .8f));
            Check(matches.Select(candidate => candidate.item.id).SequenceEqual(
                new[] { "cardboard_box", "milk_carton", "aseptic_carton" }),
                "Carton should not be interpreted only as a milk or juice pack.");
        });
        Run("document on a box does not outrank the concrete cardboard evidence", () =>
        {
            var matches = WasteRecognition.Match(MatchingCatalog(), new[]
            {
                new RecognitionSignal { identifier = "document", confidence = .96f },
                new RecognitionSignal { identifier = "corrugated cardboard box", confidence = .43f }
            });
            Check(matches.Select(candidate => candidate.item.id).SequenceEqual(
                new[] { "cardboard_box", "office_paper" }),
                "A printed shipping label displaced the cardboard box guide.");
            Check(matches[0].confidence == .43f && matches[1].confidence == .96f,
                "Suggestion ranking must not relabel scores as higher confidence.");
        });
        Run("document on a beverage can does not displace both can alternatives", () =>
        {
            var matches = WasteRecognition.Match(MatchingCatalog(), new[]
            {
                new RecognitionSignal { identifier = "document", confidence = .93f },
                new RecognitionSignal { identifier = "cola can", confidence = .42f }
            }, 2);
            Check(matches.Select(candidate => candidate.item.id).SequenceEqual(
                new[] { "beverage_can", "food_can" }),
                "Label text displaced the concrete can suggestions within the candidate limit.");
        });
        Run("ordinary paper remains available when no container is detected", () =>
        {
            var matches = WasteRecognition.Match(MatchingCatalog(), Signals("document", .8f));
            Check(matches.Length == 1 && matches[0].item.id == "office_paper",
                "Paper fallback was removed instead of ranked below container evidence.");
        });
        Run("unrelated weak object evidence does not displace ordinary paper", () =>
        {
            var matches = WasteRecognition.Match(MatchingCatalog(), new[]
            {
                new RecognitionSignal { identifier = "document", confidence = .95f },
                new RecognitionSignal { identifier = "button", confidence = .3f },
                new RecognitionSignal { identifier = "cardboard box", confidence = .24f }
            });
            Check(matches.Select(candidate => candidate.item.id).SequenceEqual(
                new[] { "office_paper", "button" }),
                "Paper ranking changed without supported container evidence above the threshold.");
        });
        Run("beverage words alone do not claim a can or bottle material", () =>
        {
            foreach (string label in new[] { "beverage", "drink", "soda", "beer", "cola", "aluminum" })
                Check(WasteRecognition.Match(MatchingCatalog(), Signals(label, .99f)).Length == 0,
                    "Beverage content alone implied a particular container: " + label);
        });
        Run("food box evidence preserves the greasy paper box decision guide", () =>
        {
            var matches = WasteRecognition.Match(new WasteCatalog(Load()), Signals("pizza box", .9f));
            Check(matches.Length == 1 && matches[0].item.id == "pizza_box",
                "A specific food box should not be flattened into ordinary cardboard or paper.");
        });
        Run("bottle label preserves PET and glass ambiguity", () =>
        {
            var matches = WasteRecognition.Match(MatchingCatalog(), Signals("water_bottle", .9f));
            Check(matches.Select(candidate => candidate.item.id).SequenceEqual(new[] { "pet_bottle", "glass_bottle" }),
                "An image alone must not definitively infer bottle material.");
        });
        Run("button label suggests the button guide", () =>
        {
            var matches = WasteRecognition.Match(MatchingCatalog(), Signals("button", .8f));
            Check(matches.Length == 1 && matches[0].item.id == "button", "Button suggestion missing.");
        });
        Run("exact catalog keywords add concrete objects without new code", () =>
        {
            var matches = WasteRecognition.Match(MatchingCatalog(), Signals("toothbrush", .8f));
            Check(matches.Length == 1 && matches[0].item.id == "toothbrush", "Concrete keyword mapping failed.");
        });
        Run("bottle caps and ice packs preserve material or contents ambiguity", () =>
        {
            var caps = WasteRecognition.Match(MatchingCatalog(), Signals("bottle_cap", .8f));
            Check(caps.Select(candidate => candidate.item.id).SequenceEqual(new[] { "pet_cap", "metal_cap" }),
                "A cap image must not establish plastic versus metal.");
            var packs = WasteRecognition.Match(MatchingCatalog(), Signals("ice pack", .8f));
            Check(packs.Select(candidate => candidate.item.id).SequenceEqual(new[] { "water_ice_pack", "gel_ice_pack" }),
                "An ice-pack image must not establish water versus gel contents.");
        });
        Run("generic cup label preserves paper plastic and ceramic alternatives", () =>
        {
            var matches = WasteRecognition.Match(MatchingCatalog(), Signals("coffee_cup", .8f));
            Check(matches.Select(candidate => candidate.item.id).SequenceEqual(new[] { "paper_cup", "plastic_cup", "ceramic" }),
                "A cup image must not establish its material.");
        });
        Run("generic food and material labels do not infer packaging", () =>
        {
            foreach (string label in new[] { "food", "dish", "plastic", "metal", "red", "oil", "container", "aluminum", "pet", "cap", "fruit peel", "tin" })
                Check(WasteRecognition.Match(MatchingCatalog(), Signals(label, .99f)).Length == 0,
                    "Generic label must not imply a disposal item: " + label);
        });
        Run("unknown and weak image labels return no suggestion", () =>
        {
            Check(WasteRecognition.Match(MatchingCatalog(), Signals("unknown_shape", .99f)).Length == 0,
                "Unknown image label must not get a default item.");
            Check(WasteRecognition.Match(MatchingCatalog(), Signals("can", .1f)).Length == 0,
                "Weak classifier score must not be presented as a useful candidate.");
        });
        Run("can and button matching cannot accidentally match longer nouns", () =>
        {
            foreach (string label in new[] { "candle", "can_opener", "watering_can", "trash_can", "button_mushroom", "bottle_opener",
                "beer can chicken", "software package", "boxing glove", "mailbox" })
                Check(WasteRecognition.Match(MatchingCatalog(), Signals(label, .99f)).Length == 0,
                    "Substring false positive: " + label);
        });
        Run("duplicate labels do not add confidence or repeat candidates", () =>
        {
            var matches = WasteRecognition.Match(MatchingCatalog(), new[]
            {
                new RecognitionSignal { identifier = "can", confidence = .7f },
                new RecognitionSignal { identifier = "soda can", confidence = .8f }
            });
            Check(matches.Select(candidate => candidate.item.id).Distinct().Count() == matches.Length,
                "Duplicate image labels must not duplicate suggestions.");
            Check(matches.All(candidate => candidate.confidence <= .8f),
                "Image labels must not be summed into an invented confidence.");
        });
        Run("invalid image scores are ignored", () =>
        {
            foreach (float score in new[] { float.NaN, float.PositiveInfinity, -1f, 2f })
                Check(WasteRecognition.Match(MatchingCatalog(), Signals("can", score)).Length == 0,
                    "Invalid classifier score was accepted.");
        });
        Run("candidate limit and configurable threshold are respected", () =>
        {
            Check(WasteRecognition.Match(MatchingCatalog(), Signals("can", .8f), 1).Length == 1,
                "Candidate limit was ignored.");
            Check(WasteRecognition.Match(MatchingCatalog(), Signals("can", .3f), 3, .5f).Length == 0,
                "Configured confidence threshold was ignored.");
            Check(WasteRecognition.Match(MatchingCatalog(), Signals("can", .8f), 0).Length == 0,
                "Zero requested candidates should return no candidates.");
        });
        Console.WriteLine("Passed " + _passed + " catalog regression checks; " + _failed + " failed.");
        return _failed == 0 ? 0 : 1;
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

    static GuideNode Node(WasteCatalogData data, string itemId, string nodeId)
    {
        return data.items.Single(item => item.id == itemId).FindNode(nodeId);
    }

    static RecognitionSignal[] Signals(string label, float confidence)
    {
        return new[] { new RecognitionSignal { identifier = label, confidence = confidence } };
    }

    static WasteCatalog MatchingCatalog()
    {
        Func<string, string[], WasteItem> item = (id, keywords) => new WasteItem
        {
            id = id, name = id, recognitionKeywords = keywords, startNodeId = "result",
            nodes = new[]
            {
                new GuideNode
                {
                    id = "result", choices = Array.Empty<GuideChoice>(),
                    result = new GuideResult
                    {
                        destination = "Test fixture", steps = new[] { "Test step" },
                        sourceUrls = new[] { "https://example.test/source" }
                    }
                }
            }
        };
        return new WasteCatalog(new WasteCatalogData
        {
            schemaVersion = 1, region = "Test fixture",
            items = new[]
            {
                item("beverage_can", new[] { "can", "soda can" }),
                item("food_can", new[] { "can", "tin can" }),
                item("pet_bottle", new[] { "bottle", "plastic bottle" }),
                item("glass_bottle", new[] { "bottle", "glass bottle" }),
                item("plastic_container", new[] { "food container", "plastic container", "food" }),
                item("button", new[] { "button" }),
                item("toothbrush", new[] { "toothbrush" }),
                item("pet_cap", new[] { "bottle cap", "cap" }),
                item("metal_cap", new[] { "metal bottle cap" }),
                item("water_ice_pack", new[] { "ice pack" }),
                item("gel_ice_pack", new[] { "gel ice pack" }),
                item("paper_cup", new[] { "paper cup", "coffee cup" }),
                item("plastic_cup", new[] { "plastic cup", "cup" }),
                item("ceramic", new[] { "ceramic mug" }),
                item("cardboard_box", new[] { "cardboard box", "carton", "box" }),
                item("milk_carton", new[] { "milk carton", "paper carton" }),
                item("aseptic_carton", new[] { "juice carton", "paper carton" }),
                item("office_paper", new[] { "paper", "sheet of paper", "document" }),
                item("shipping_bag", new[] { "plastic mailer" })
            }
        });
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
        try
        {
            test();
            _passed++;
            Console.WriteLine("PASS " + label);
        }
        catch (Exception exception)
        {
            _failed++;
            Console.Error.WriteLine("FAIL " + label + ": " + exception.Message);
        }
    }
}
