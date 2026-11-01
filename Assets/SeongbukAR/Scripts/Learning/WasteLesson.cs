using System;

namespace SeongbukAR.Learning
{
    // Instructions describe a teaching example, not a judgement about the
    // material or cleanliness of a real object seen by the camera.
    public sealed class WasteLessonStep
    {
        public string Title { get; }
        public string Instruction { get; }

        internal WasteLessonStep(string title, string instruction)
        {
            Title = title;
            Instruction = instruction;
        }
    }

    // Tracking can report the same image every frame, or reacquire it after
    // losing sight of it. Selecting that image must not reset the lesson.
    public sealed class WasteLessonState
    {
        static readonly WasteLessonStep[] PetBottle =
        {
            new WasteLessonStep("전체 보기",
                "내용물을 비우고 가볍게 헹군 뒤 물기를 빼 주세요. 투명한 생수·음료 페트병을 예로 든 학습입니다."),
            new WasteLessonStep("뚜껑 열기",
                "압착하기 전에 뚜껑을 열어 공기가 빠져나갈 수 있게 해 주세요. 뚜껑은 버리지 않고 마지막 단계에서 다시 닫습니다."),
            new WasteLessonStep("라벨 제거",
                "라벨을 떼어 몸통과 구분해 보세요. 라벨은 재질이 다를 수 있으므로 표시와 지역 수거 기준을 확인하세요."),
            new WasteLessonStep("몸통 압착·뚜껑 닫기",
                "빈 몸통을 압착한 뒤 뚜껑을 다시 닫아 주세요. 투명한 생수·음료 페트병은 다른 플라스틱과 구분해 별도 배출하고, 떼어 낸 라벨은 표시된 재질과 지역 수거 안내에 따라 배출하세요.")
        };

        static readonly WasteLessonStep[] PlasticContainer =
        {
            new WasteLessonStep("전체 보기",
                "음식물을 비우고 용기와 뚜껑을 살펴보세요. 배달 용기를 예로 분리배출 준비 과정을 배웁니다."),
            new WasteLessonStep("뚜껑 분리",
                "뚜껑을 열어 용기와 분리해 보세요. 용기와 뚜껑은 서로 다른 재질일 수 있습니다."),
            new WasteLessonStep("오염 확인",
                "음식물·기름이 남았는지 확인하세요. 제거할 수 있다면 세척하고, 오염을 제거하기 어렵다면 재활용 가능 여부를 지역 수거 안내로 확인하세요."),
            new WasteLessonStep("배출 준비",
                "용기와 뚜껑의 재질 표시를 각각 확인하세요. 세척 상태와 복합재질 여부에 따라 배출 방법이 달라지므로 지역 수거 안내를 따라 주세요.")
        };

        WasteLessonStep[] _steps = Array.Empty<WasteLessonStep>();

        public string ItemId { get; private set; }
        public int StepIndex { get; private set; } = -1;
        public int StepCount => _steps.Length;
        public WasteLessonStep CurrentStep => StepIndex >= 0 ? _steps[StepIndex] : null;
        // Completion refers only to reaching the final teaching step.
        public bool IsComplete => StepCount > 0 && StepIndex == StepCount - 1;

        public static bool Supports(string itemId) =>
            itemId == "pet_bottle" || itemId == "plastic_container";

        public bool Select(string itemId)
        {
            WasteLessonStep[] steps;
            switch (itemId)
            {
                case "pet_bottle": steps = PetBottle; break;
                case "plastic_container": steps = PlasticContainer; break;
                default: return false;
            }
            if (ItemId == itemId) return true;
            ItemId = itemId;
            _steps = steps;
            StepIndex = 0;
            return true;
        }

        public bool Next()
        {
            if (StepIndex < 0 || StepIndex >= StepCount - 1) return false;
            StepIndex++;
            return true;
        }

        public bool Previous()
        {
            if (StepIndex <= 0) return false;
            StepIndex--;
            return true;
        }

        public void Restart()
        {
            if (StepCount > 0) StepIndex = 0;
        }

        public void Clear()
        {
            ItemId = null;
            _steps = Array.Empty<WasteLessonStep>();
            StepIndex = -1;
        }
    }
}
