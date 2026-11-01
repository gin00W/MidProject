using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SeongbukAR
{
    [Serializable]
    public sealed class RecognitionSignal
    {
        public string identifier;
        public float confidence;
    }

    public sealed class RecognitionCandidate
    {
        public WasteItem item;
        // The original image-classification score, not a calibrated probability
        // that the material or disposal method is correct. Always confirm it.
        public float confidence;
        public string evidence;
    }

    // Converts real classifier labels into suggestions for user confirmation.
    // It does not infer grease removability, material composition or bin type.
    public static class WasteRecognition
    {
        public const float DefaultMinimumConfidence = 0.25f;

        static readonly HashSet<string> GenericLabels = new HashSet<string>(StringComparer.Ordinal)
        {
            "food", "meal", "dish", "tableware", "kitchenware", "object", "product",
            "container", "packaging", "plastic", "metal", "glass", "paper", "garbage",
            "trash", "waste", "recycling", "drink", "beverage", "water", "liquid",
            "red", "oil", "grease", "oily", "greasy", "aluminum", "aluminium", "steel",
            "iron", "tin", "pet", "pp", "pe", "polypropylene", "polystyrene",
            "prepared food", "cuisine", "fast food", "junk food", "takeout", "takeaway food",
            "cap", "lid", "cover", "top", "tube", "fruit peel"
        };

        static readonly string[] ExcludedPhrases =
        {
            "can opener", "bottle opener", "bottle brush", "watering can", "trash can",
            "garbage can", "recycling bin", "button mushroom", "button down", "button up",
            "button switch"
        };

        sealed class CandidateScore
        {
            public RecognitionCandidate candidate;
            public int priority;
        }

        public static RecognitionCandidate[] Match(WasteCatalog catalog,
            IEnumerable<RecognitionSignal> labels, int limit = 3,
            float minimumConfidence = DefaultMinimumConfidence)
        {
            if (float.IsNaN(minimumConfidence) || float.IsInfinity(minimumConfidence) ||
                minimumConfidence < 0 || minimumConfidence > 1)
                throw new ArgumentOutOfRangeException(nameof(minimumConfidence));
            if (catalog == null || labels == null || limit <= 0)
                return Array.Empty<RecognitionCandidate>();

            var scores = new Dictionary<string, CandidateScore>(StringComparer.Ordinal);
            foreach (var signal in labels)
            {
                if (signal == null || string.IsNullOrWhiteSpace(signal.identifier) ||
                    float.IsNaN(signal.confidence) || float.IsInfinity(signal.confidence) ||
                    signal.confidence < minimumConfidence || signal.confidence > 1)
                    continue;

                string normalized = Normalize(signal.identifier);
                if (ExcludedPhrases.Any(phrase => ContainsPhrase(normalized, phrase))) continue;

                foreach (string label in IdentifierVariants(signal.identifier))
                {
                    if (GenericLabels.Contains(label)) continue;
                    AddKnownCandidates(catalog, label, signal, scores);

                    // Data can add further concrete object labels without new
                    // code, but no substring matching or broad material guess.
                    foreach (var item in catalog.Data.items)
                    {
                        if ((item.recognitionKeywords ?? Array.Empty<string>())
                            .Any(keyword => Normalize(keyword) == label))
                            Add(item, signal, 30, scores);
                    }
                }
            }

            bool hasContainerEvidence = scores.Keys.Any(id => Is(id, "beverage_can", "food_can",
                "cardboard_box", "milk_carton", "aseptic_carton", "pizza_box", "pet_bottle",
                "glass_bottle", "plastic_container", "shipping_bag"));

            return scores.Values
                // A printed shipping label can be classified as a document.
                // Keep ordinary paper as a fallback only when the photograph
                // also identifies a supported container or box. Other object
                // labels retain score order against ordinary paper.
                // This changes suggestion order, never the classifier score.
                .OrderBy(score => hasContainerEvidence && score.candidate.item.id == "office_paper")
                .ThenByDescending(score => score.candidate.confidence)
                .ThenByDescending(score => score.priority)
                .ThenBy(score => score.candidate.item.id, StringComparer.Ordinal)
                .Take(limit)
                .Select(score => score.candidate)
                .ToArray();
        }

        static void AddKnownCandidates(WasteCatalog catalog, string label,
            RecognitionSignal signal, Dictionary<string, CandidateScore> scores)
        {
            if (Is(label, "can", "cans", "tin can", "tin cans", "tincan", "tincans",
                "metal can", "metal cans", "aluminum can", "aluminium can", "aluminum cans", "aluminium cans"))
                AddIds(catalog, signal, scores, "beverage_can", "food_can");
            else if (Is(label, "soda can", "soda cans", "beverage can", "beverage cans", "beer can", "beer cans",
                "drink can", "drink cans", "soft drink can", "soft drink cans", "cola can", "cola cans",
                "coke can", "coke cans", "pop can", "pop cans", "soda pop can", "pull tab can", "pop top can",
                "aluminum beverage can", "aluminium beverage can", "aluminum drink can", "aluminium drink can",
                "aluminum soda can", "aluminium soda can", "aluminum beer can", "aluminium beer can"))
                AddIds(catalog, signal, scores, "beverage_can", "food_can");
            else if (Is(label, "food can", "food cans", "canned food", "canned goods", "soup can", "tuna can"))
                AddIds(catalog, signal, scores, "food_can", "beverage_can");
            else if (Is(label, "bottle", "plastic bottle", "water bottle", "soda bottle", "beverage bottle",
                "glass bottle", "wine bottle", "beer bottle", "pet bottle", "pop bottle"))
            {
                // An image label is insufficient to establish the material.
                // Keep both PET and glass as named, confirmable possibilities.
                if (Is(label, "glass bottle", "wine bottle", "beer bottle"))
                    AddIds(catalog, signal, scores, "glass_bottle", "pet_bottle");
                else
                    AddIds(catalog, signal, scores, "pet_bottle", "glass_bottle");
            }
            else if (Is(label, "food container", "takeout container", "take out container", "takeaway container",
                "plastic food container", "plastic container", "food storage container"))
                AddIds(catalog, signal, scores, "plastic_container");
            else if (Is(label, "jar", "glass jar"))
                AddIds(catalog, signal, scores, "glass_bottle", "plastic_container");
            else if (Is(label, "bottle cap", "plastic bottle cap", "metal bottle cap", "crown cap"))
                AddIds(catalog, signal, scores, "pet_cap", "metal_cap");
            else if (Is(label, "cup", "coffee cup", "drinking cup", "mug"))
                AddIds(catalog, signal, scores, "paper_cup", "plastic_cup", "ceramic");
            else if (Is(label, "bowl", "food bowl", "food tray"))
                AddIds(catalog, signal, scores, "plastic_container", "ceramic");
            else if (Is(label, "ice pack"))
                AddIds(catalog, signal, scores, "water_ice_pack", "gel_ice_pack");
            else if (Is(label, "carton", "cartons"))
                AddIds(catalog, signal, scores, "cardboard_box", "milk_carton", "aseptic_carton");
            else if (Is(label, "paper carton"))
                AddIds(catalog, signal, scores, "milk_carton", "aseptic_carton", "cardboard_box");
            else if (Is(label, "box", "boxes"))
                // "Box" identifies a shape, not its material. The user must
                // choose the named object before any disposal advice is given.
                AddIds(catalog, signal, scores, "cardboard_box", "plastic_container");
            else if (Is(label, "parcel", "package", "shipping package", "postal package"))
                AddIds(catalog, signal, scores, "cardboard_box", "shipping_bag");
            else if (Is(label, "button", "clothing button", "garment button", "sewing button"))
                AddIds(catalog, signal, scores, "button");
            else if (Is(label, "battery", "batteries", "dry cell", "alkaline battery", "rechargeable battery",
                "lithium battery"))
                AddIds(catalog, signal, scores, "battery");
            else if (Is(label, "power bank", "portable charger"))
                AddIds(catalog, signal, scores, "power_bank");
            else if (Is(label, "clothing", "clothes", "garment", "shirt", "t shirt", "jacket", "pants",
                "trousers", "dress", "coat", "sweater", "jeans", "skirt"))
                AddIds(catalog, signal, scores, "clothing");
            else if (Is(label, "electronic device", "electronics", "electrical appliance", "appliance",
                "hair dryer", "hairdryer", "toaster", "electric kettle", "vacuum cleaner", "electric fan"))
                AddIds(catalog, signal, scores, "small_appliance");
            else if (Is(label, "light bulb", "lightbulb", "lamp bulb"))
                AddIds(catalog, signal, scores, "led_lamp", "fluorescent_lamp", "incandescent_bulb");
            else if (Is(label, "fluorescent lamp", "fluorescent light", "fluorescent tube"))
                AddIds(catalog, signal, scores, "fluorescent_lamp", "led_lamp");
            else if (Is(label, "led bulb", "led lamp", "led light"))
                AddIds(catalog, signal, scores, "led_lamp", "fluorescent_lamp");
            else if (Is(label, "cardboard", "cardboard box", "cardboard boxes", "corrugated box",
                "corrugated cardboard", "corrugated cardboard box", "corrugated carton", "cardboard carton",
                "cardboard packaging", "shipping box", "shipping carton", "carton box", "packing box",
                "packaging box", "paperboard box"))
                AddIds(catalog, signal, scores, "cardboard_box");
        }

        static void AddIds(WasteCatalog catalog, RecognitionSignal signal,
            Dictionary<string, CandidateScore> scores, params string[] ids)
        {
            for (int i = 0; i < ids.Length; i++)
            {
                var item = catalog.Find(ids[i]);
                if (item != null) Add(item, signal, 100 - i, scores);
            }
        }

        static void Add(WasteItem item, RecognitionSignal signal, int priority,
            Dictionary<string, CandidateScore> scores)
        {
            CandidateScore previous;
            if (scores.TryGetValue(item.id, out previous) &&
                (previous.candidate.confidence > signal.confidence ||
                 (previous.candidate.confidence == signal.confidence && previous.priority >= priority)))
                return;

            scores[item.id] = new CandidateScore
            {
                priority = priority,
                candidate = new RecognitionCandidate
                {
                    item = item,
                    confidence = signal.confidence,
                    evidence = signal.identifier
                }
            };
        }

        static IEnumerable<string> IdentifierVariants(string identifier)
        {
            return identifier.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(Normalize).Where(value => value.Length > 0).Distinct(StringComparer.Ordinal);
        }

        static bool Is(string value, params string[] alternatives) =>
            Array.IndexOf(alternatives, value) >= 0;

        static bool ContainsPhrase(string value, string phrase) =>
            (" " + value + " ").Contains(" " + phrase + " ");

        static string Normalize(string value)
        {
            var result = new StringBuilder();
            bool pendingSpace = false;
            foreach (char character in (value ?? "").Normalize(NormalizationForm.FormKC).ToLowerInvariant())
            {
                if (char.IsLetterOrDigit(character))
                {
                    if (pendingSpace && result.Length > 0) result.Append(' ');
                    result.Append(character);
                    pendingSpace = false;
                }
                else pendingSpace = true;
            }
            return result.ToString();
        }
    }
}
