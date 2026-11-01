# Chess typography

The chess app, its world-space piece labels and the printable sheets use the
unmodified static Pretendard Regular (400) and SemiBold (600) fonts in this folder.
The SemiBold file supplies the weight directly; the UI does not synthesize bold.

- Official upstream: https://github.com/orioncactus/pretendard
- Embedded font version: `Version 1.309;Glyphs 3.2 (3225)`
- These files are copied without modification from the existing project's
  `Assets/SeongbukAR/Resources/Fonts/` directory. A release tag is not inferred
  from the embedded version alone.
- License: SIL Open Font License 1.1. Full copyright notices and license are
  included in `LICENSE.txt`.

| File | Weight | SHA-256 |
| --- | --- | --- |
| Pretendard-Regular.otf | 400 | `3ffbacde6ab8411f1d2db54bb9b1f0b3ee2a738932033722cf0388c06aed1c93` |
| Pretendard-SemiBold.otf | 600 | `c89bc43027dc7cde5726e96223376f8eec09302b2fc1f8147fd5b57cfc376118` |

Both fonts cover all 11,172 modern Hangul syllables. Runtime Resources paths are
`Chess/Fonts/Pretendard-Regular` and `Chess/Fonts/Pretendard-SemiBold`.
