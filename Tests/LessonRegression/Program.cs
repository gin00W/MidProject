using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using SeongbukAR.Learning;

static class Program
{
    static int _passed;
    static int _failed;
    static readonly string[] Items = { "pet_bottle", "plastic_container" };

    static int Main()
    {
        Run("an empty lesson cannot advance or claim completion", () =>
        {
            var state = new WasteLessonState();
            CheckEmpty(state);
            Check(!state.Next() && !state.Previous(), "An empty lesson navigated.");
            state.Restart();
            CheckEmpty(state);
        });
        Run("both registered images open a four-stage lesson at the beginning", () =>
        {
            foreach (string id in Items)
            {
                var state = new WasteLessonState();
                Check(state.Select(id) && state.ItemId == id, "Image did not select its lesson: " + id);
                Check(state.StepIndex == 0 && state.StepCount == 4 && state.CurrentStep != null &&
                    !state.IsComplete, "Selected lesson did not begin at its first stage.");
            }
        });
        Run("progression stops at the final stage without wrapping or skipping", () =>
        {
            foreach (string id in Items)
            {
                var state = new WasteLessonState();
                state.Select(id);
                for (int expected = 1; expected < 4; expected++)
                {
                    Check(state.Next() && state.StepIndex == expected, "Next skipped a stage.");
                    Check(state.IsComplete == (expected == 3), "Completion changed at the wrong stage.");
                }
                var final = state.CurrentStep;
                Check(!state.Next() && !state.Next() && state.StepIndex == 3 &&
                    ReferenceEquals(final, state.CurrentStep), "Final-stage navigation changed the lesson.");
            }
        });
        Run("going back clears completion and stops at the beginning", () =>
        {
            var state = Complete("pet_bottle");
            for (int expected = 2; expected >= 0; expected--)
                Check(state.Previous() && state.StepIndex == expected && !state.IsComplete,
                    "Previous did not restore the expected stage.");
            Check(!state.Previous() && state.StepIndex == 0, "Previous moved before the first stage.");
        });
        Run("continuous reports and reacquisition of one image preserve progress", () =>
        {
            var state = new WasteLessonState();
            state.Select("pet_bottle");
            state.Next();
            state.Next();
            var current = state.CurrentStep;
            for (int frame = 0; frame < 120; frame++)
                Check(state.Select("pet_bottle") && state.StepIndex == 2 &&
                    ReferenceEquals(current, state.CurrentStep), "Tracking reset the learner's progress.");
        });
        Run("reacquiring a completed image does not restart its animation", () =>
        {
            foreach (string id in Items)
            {
                var state = Complete(id);
                Check(state.Select(id) && state.StepIndex == 3 && state.IsComplete,
                    "Reacquisition reset a completed lesson.");
            }
        });
        Run("switching the registered image starts that object's lesson", () =>
        {
            var state = Complete("pet_bottle");
            Check(state.Select("plastic_container") && state.ItemId == "plastic_container" &&
                state.StepIndex == 0 && !state.IsComplete && state.CurrentStep.Title == "전체 보기",
                "New image retained the previous object's stage.");
            state.Next();
            Check(state.Select("pet_bottle") && state.StepIndex == 0,
                "Returning from another object did not start the selected lesson.");
        });
        Run("an unrelated or malformed image name cannot replace the current lesson", () =>
        {
            var state = Complete("pet_bottle");
            var current = state.CurrentStep;
            foreach (string id in new[] { null, "", " ", "beverage_can", "PET_BOTTLE", "pet_bottle.png" })
                Check(!state.Select(id) && state.ItemId == "pet_bottle" && state.StepIndex == 3 &&
                    state.IsComplete && ReferenceEquals(current, state.CurrentStep),
                    "Unknown image mutated the current lesson.");
            var empty = new WasteLessonState();
            Check(!empty.Select("unknown"), "Unknown image was accepted.");
            CheckEmpty(empty);
        });
        Run("restart rewinds the selected object without losing its lesson", () =>
        {
            foreach (string id in Items)
            {
                var state = Complete(id);
                state.Restart();
                Check(state.ItemId == id && state.StepIndex == 0 && state.StepCount == 4 &&
                    !state.IsComplete, "Restart lost the selection or retained completion.");
                state.Restart();
                Check(state.StepIndex == 0 && state.Next() && state.StepIndex == 1,
                    "A restarted lesson could not run again.");
            }
        });
        Run("leaving a lesson clears state and permits a fresh selection", () =>
        {
            var state = Complete("plastic_container");
            state.Clear();
            state.Clear();
            CheckEmpty(state);
            Check(!state.Next() && !state.Previous(), "Cleared state could still advance.");
            Check(state.Select("plastic_container") && state.StepIndex == 0 && !state.IsComplete,
                "A new session retained the old progress.");
        });
        Run("lesson identities match real catalog entries and marker image files", () =>
        {
            string root = FindRoot();
            using var catalog = JsonDocument.Parse(File.ReadAllText(Path.Combine(root,
                "Assets/SeongbukAR/Resources/Data/seongbuk-demo.json")));
            var ids = catalog.RootElement.GetProperty("items").EnumerateArray()
                .Select(item => item.GetProperty("id").GetString()).ToArray();
            foreach (string id in Items)
            {
                Check(ids.Contains(id), "Lesson ID is absent from the guide catalog: " + id);
                Check(File.Exists(Path.Combine(root, "Assets/SeongbukAR/Art", id + ".png")),
                    "Lesson does not have its corresponding image target: " + id);
            }
        });
        Run("each reachable stage provides a specific teaching instruction", () =>
        {
            foreach (string id in Items)
            {
                var state = new WasteLessonState();
                state.Select(id);
                string previous = null;
                do
                {
                    Check(!string.IsNullOrWhiteSpace(state.CurrentStep.Title) &&
                        !string.IsNullOrWhiteSpace(state.CurrentStep.Instruction), "A stage cannot explain its animation.");
                    Check(previous != state.CurrentStep.Instruction, "Advancing did not change the explanation.");
                    previous = state.CurrentStep.Instruction;
                } while (state.Next());
                Check(state.IsComplete, "One lesson could not reach completion.");
            }
        });
        Run("the PET animation ends with the cap closed as required by the catalog", () =>
        {
            using var catalog = JsonDocument.Parse(File.ReadAllText(Path.Combine(FindRoot(),
                "Assets/SeongbukAR/Resources/Data/seongbuk-demo.json")));
            var pet = catalog.RootElement.GetProperty("items").EnumerateArray()
                .Single(item => item.GetProperty("id").GetString() == "pet_bottle");
            var result = pet.GetProperty("nodes").EnumerateArray()
                .Single(node => node.GetProperty("id").GetString() == "clear_pet").GetProperty("result");
            string catalogSteps = string.Join(" ", result.GetProperty("steps").EnumerateArray()
                .Select(step => step.GetString()));
            Check(catalogSteps.Contains("압착") && catalogSteps.Contains("뚜껑을 다시 닫"),
                "Catalog changed its cap handling; review the corresponding animation and lesson.");
            var state = Complete("pet_bottle");
            Check(state.CurrentStep.Instruction.Contains("압착한 뒤 뚜껑을 다시 닫") &&
                state.CurrentStep.Instruction.Contains("다른 플라스틱과 구분"),
                "Lesson would teach disposal with the cap removed or mix transparent PET with other plastics.");
        });
        Console.WriteLine("Passed " + _passed + " lesson checks; failed " + _failed + ".");
        return _failed == 0 ? 0 : 1;
    }

    static WasteLessonState Complete(string id)
    {
        var state = new WasteLessonState();
        Check(state.Select(id), "Could not select lesson fixture.");
        while (state.Next()) { }
        Check(state.IsComplete, "Lesson did not reach its final stage.");
        return state;
    }

    static void CheckEmpty(WasteLessonState state)
    {
        Check(state.ItemId == null && state.StepIndex == -1 && state.StepCount == 0 &&
            state.CurrentStep == null && !state.IsComplete, "Empty state retained lesson information.");
    }

    static string FindRoot()
    {
        foreach (string start in new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory })
            for (var path = new DirectoryInfo(start); path != null; path = path.Parent)
                if (File.Exists(Path.Combine(path.FullName, "Assets/SeongbukAR/Resources/Data/seongbuk-demo.json")))
                    return path.FullName;
        throw new FileNotFoundException("Repository catalog not found for the marker/catalog integration check.");
    }

    static void Run(string label, Action test)
    {
        try { test(); _passed++; Console.WriteLine("PASS " + label); }
        catch (Exception error) { _failed++; Console.WriteLine("FAIL " + label + ": " + error.Message); }
    }

    static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }
}
