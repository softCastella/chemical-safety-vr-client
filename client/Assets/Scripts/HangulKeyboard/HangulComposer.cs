using System;
using System.Collections.Generic;
using System.Text;

namespace Prototype.Tyche.UI.Keyboard
{
    /// <summary>
    /// Clean-room two-set Korean (두벌식) composer based on the Unicode Hangul syllable formula.
    /// The composer stores only the active run of physical QWERTY keys so Backspace can replay
    /// every intermediate composition state deterministically.
    /// </summary>
    public sealed class HangulComposer
    {
        const int HangulSyllableBase = 0xAC00;

        const string InitialCompatibilityJamo = "ㄱㄲㄴㄷㄸㄹㅁㅂㅃㅅㅆㅇㅈㅉㅊㅋㅌㅍㅎ";
        const string MedialCompatibilityJamo = "ㅏㅐㅑㅒㅓㅔㅕㅖㅗㅘㅙㅚㅛㅜㅝㅞㅟㅠㅡㅢㅣ";

        static readonly Dictionary<char, JamoKey> s_KeyMap = CreateKeyMap();

        readonly List<char> m_Keys = new List<char>();
        string m_RenderedText = string.Empty;

        /// <summary>The text currently produced by the active Korean key run.</summary>
        public string renderedText => m_RenderedText;

        /// <summary>True while the composer has Korean keys that can be decomposed by Backspace.</summary>
        public bool hasInput => m_Keys.Count > 0;

        /// <summary>Number of physical two-set keys in the active run.</summary>
        public int keyCount => m_Keys.Count;

        /// <summary>Returns whether a physical QWERTY key is part of the two-set Korean layout.</summary>
        public static bool IsSupportedKey(char key)
        {
            return TryGetJamoKey(key, out _);
        }

        /// <summary>Adds a physical two-set key and recomposes the active run.</summary>
        public bool TryAppend(char key)
        {
            if (!TryGetJamoKey(key, out _))
                return false;

            m_Keys.Add(key);
            m_RenderedText = Compose(m_Keys);
            return true;
        }

        /// <summary>Removes the most recent physical key and recomposes the active run.</summary>
        public bool TryBackspace(out char removedKey)
        {
            if (m_Keys.Count == 0)
            {
                removedKey = default;
                return false;
            }

            var lastIndex = m_Keys.Count - 1;
            removedKey = m_Keys[lastIndex];
            m_Keys.RemoveAt(lastIndex);
            m_RenderedText = Compose(m_Keys);
            return true;
        }

        /// <summary>Commits the visible result by clearing only the internal composition history.</summary>
        public void Clear()
        {
            m_Keys.Clear();
            m_RenderedText = string.Empty;
        }

        /// <summary>Utility used by validation code to compose a complete physical-key sequence.</summary>
        public static string ComposeKeys(string keys)
        {
            if (keys == null)
                throw new ArgumentNullException(nameof(keys));

            var composer = new HangulComposer();
            foreach (var key in keys)
            {
                if (!composer.TryAppend(key))
                    throw new ArgumentException($"Unsupported two-set key: '{key}'.", nameof(keys));
            }

            return composer.renderedText;
        }

        static string Compose(IReadOnlyList<char> keys)
        {
            if (keys.Count == 0)
                return string.Empty;

            var completed = new StringBuilder(keys.Count);
            var initial = -1;
            var medial = -1;
            var final = 0;

            for (var index = 0; index < keys.Count; ++index)
            {
                if (!TryGetJamoKey(keys[index], out var key))
                    continue;

                if (key.isVowel)
                {
                    ProcessVowel(key.medial, completed, ref initial, ref medial, ref final);
                }
                else
                {
                    ProcessConsonant(key.initial, key.final, completed, ref initial, ref medial, ref final);
                }
            }

            AppendCurrent(completed, initial, medial, final);
            return completed.ToString();
        }

        static void ProcessConsonant(
            int newInitial,
            int newFinal,
            StringBuilder completed,
            ref int initial,
            ref int medial,
            ref int final)
        {
            if (initial < 0 && medial < 0)
            {
                initial = newInitial;
                return;
            }

            if (initial >= 0 && medial < 0)
            {
                AppendCurrent(completed, initial, medial, final);
                initial = newInitial;
                medial = -1;
                final = 0;
                return;
            }

            if (initial < 0 && medial >= 0)
            {
                AppendCurrent(completed, initial, medial, final);
                initial = newInitial;
                medial = -1;
                final = 0;
                return;
            }

            if (final == 0)
            {
                if (newFinal > 0)
                {
                    final = newFinal;
                }
                else
                {
                    AppendCurrent(completed, initial, medial, final);
                    initial = newInitial;
                    medial = -1;
                    final = 0;
                }

                return;
            }

            if (TryCombineFinal(final, newFinal, out var combinedFinal))
            {
                final = combinedFinal;
                return;
            }

            AppendCurrent(completed, initial, medial, final);
            initial = newInitial;
            medial = -1;
            final = 0;
        }

        static void ProcessVowel(
            int newMedial,
            StringBuilder completed,
            ref int initial,
            ref int medial,
            ref int final)
        {
            if (initial < 0 && medial < 0)
            {
                medial = newMedial;
                return;
            }

            if (initial >= 0 && medial < 0)
            {
                medial = newMedial;
                return;
            }

            if (initial < 0)
            {
                if (TryCombineMedial(medial, newMedial, out var combinedMedial))
                {
                    medial = combinedMedial;
                }
                else
                {
                    AppendCurrent(completed, initial, medial, final);
                    initial = -1;
                    medial = newMedial;
                    final = 0;
                }

                return;
            }

            if (final == 0)
            {
                if (TryCombineMedial(medial, newMedial, out var combinedMedial))
                {
                    medial = combinedMedial;
                }
                else
                {
                    AppendCurrent(completed, initial, medial, final);
                    initial = -1;
                    medial = newMedial;
                    final = 0;
                }

                return;
            }

            if (TrySplitCompoundFinal(final, out var remainingFinal, out var nextInitial))
            {
                completed.Append(ComposeSyllable(initial, medial, remainingFinal));
            }
            else
            {
                nextInitial = FinalToInitial(final);
                completed.Append(ComposeSyllable(initial, medial, 0));
            }

            initial = nextInitial;
            medial = newMedial;
            final = 0;
        }

        static void AppendCurrent(StringBuilder builder, int initial, int medial, int final)
        {
            if (initial >= 0 && medial >= 0)
            {
                builder.Append(ComposeSyllable(initial, medial, final));
            }
            else if (initial >= 0)
            {
                builder.Append(InitialCompatibilityJamo[initial]);
            }
            else if (medial >= 0)
            {
                builder.Append(MedialCompatibilityJamo[medial]);
            }
        }

        static char ComposeSyllable(int initial, int medial, int final)
        {
            return (char)(HangulSyllableBase + ((initial * 21) + medial) * 28 + final);
        }

        static bool TryCombineMedial(int first, int second, out int combined)
        {
            combined = -1;
            switch (first)
            {
                case 8 when second == 0: combined = 9; return true;   // ㅗ + ㅏ = ㅘ
                case 8 when second == 1: combined = 10; return true;  // ㅗ + ㅐ = ㅙ
                case 8 when second == 20: combined = 11; return true; // ㅗ + ㅣ = ㅚ
                case 13 when second == 4: combined = 14; return true; // ㅜ + ㅓ = ㅝ
                case 13 when second == 5: combined = 15; return true; // ㅜ + ㅔ = ㅞ
                case 13 when second == 20: combined = 16; return true;// ㅜ + ㅣ = ㅟ
                case 18 when second == 20: combined = 19; return true;// ㅡ + ㅣ = ㅢ
                default: return false;
            }
        }

        static bool TryCombineFinal(int first, int second, out int combined)
        {
            combined = 0;
            if (second == 0)
                return false;

            switch (first)
            {
                case 1 when second == 19: combined = 3; return true;  // ㄱ + ㅅ = ㄳ
                case 4 when second == 22: combined = 5; return true;  // ㄴ + ㅈ = ㄵ
                case 4 when second == 27: combined = 6; return true;  // ㄴ + ㅎ = ㄶ
                case 8 when second == 1: combined = 9; return true;   // ㄹ + ㄱ = ㄺ
                case 8 when second == 16: combined = 10; return true;// ㄹ + ㅁ = ㄻ
                case 8 when second == 17: combined = 11; return true;// ㄹ + ㅂ = ㄼ
                case 8 when second == 19: combined = 12; return true;// ㄹ + ㅅ = ㄽ
                case 8 when second == 25: combined = 13; return true;// ㄹ + ㅌ = ㄾ
                case 8 when second == 26: combined = 14; return true;// ㄹ + ㅍ = ㄿ
                case 8 when second == 27: combined = 15; return true;// ㄹ + ㅎ = ㅀ
                case 17 when second == 19: combined = 18; return true;// ㅂ + ㅅ = ㅄ
                default: return false;
            }
        }

        static bool TrySplitCompoundFinal(int compound, out int first, out int secondInitial)
        {
            first = 0;
            secondInitial = -1;
            switch (compound)
            {
                case 3: first = 1; secondInitial = 9; return true;   // ㄳ -> ㄱ + ㅅ
                case 5: first = 4; secondInitial = 12; return true;  // ㄵ -> ㄴ + ㅈ
                case 6: first = 4; secondInitial = 18; return true;  // ㄶ -> ㄴ + ㅎ
                case 9: first = 8; secondInitial = 0; return true;   // ㄺ -> ㄹ + ㄱ
                case 10: first = 8; secondInitial = 6; return true;  // ㄻ -> ㄹ + ㅁ
                case 11: first = 8; secondInitial = 7; return true;  // ㄼ -> ㄹ + ㅂ
                case 12: first = 8; secondInitial = 9; return true;  // ㄽ -> ㄹ + ㅅ
                case 13: first = 8; secondInitial = 16; return true; // ㄾ -> ㄹ + ㅌ
                case 14: first = 8; secondInitial = 17; return true; // ㄿ -> ㄹ + ㅍ
                case 15: first = 8; secondInitial = 18; return true; // ㅀ -> ㄹ + ㅎ
                case 18: first = 17; secondInitial = 9; return true; // ㅄ -> ㅂ + ㅅ
                default: return false;
            }
        }

        static int FinalToInitial(int final)
        {
            switch (final)
            {
                case 1: return 0;   // ㄱ
                case 2: return 1;   // ㄲ
                case 4: return 2;   // ㄴ
                case 7: return 3;   // ㄷ
                case 8: return 5;   // ㄹ
                case 16: return 6;  // ㅁ
                case 17: return 7;  // ㅂ
                case 19: return 9;  // ㅅ
                case 20: return 10; // ㅆ
                case 21: return 11; // ㅇ
                case 22: return 12; // ㅈ
                case 23: return 14; // ㅊ
                case 24: return 15; // ㅋ
                case 25: return 16; // ㅌ
                case 26: return 17; // ㅍ
                case 27: return 18; // ㅎ
                default: return -1;
            }
        }

        static bool TryGetJamoKey(char key, out JamoKey jamo)
        {
            if (s_KeyMap.TryGetValue(key, out jamo))
                return true;

            return s_KeyMap.TryGetValue(char.ToLowerInvariant(key), out jamo);
        }

        static Dictionary<char, JamoKey> CreateKeyMap()
        {
            return new Dictionary<char, JamoKey>
            {
                ['r'] = JamoKey.Consonant(0, 1),   // ㄱ
                ['R'] = JamoKey.Consonant(1, 2),   // ㄲ
                ['s'] = JamoKey.Consonant(2, 4),   // ㄴ
                ['e'] = JamoKey.Consonant(3, 7),   // ㄷ
                ['E'] = JamoKey.Consonant(4, 0),   // ㄸ
                ['f'] = JamoKey.Consonant(5, 8),   // ㄹ
                ['a'] = JamoKey.Consonant(6, 16),  // ㅁ
                ['q'] = JamoKey.Consonant(7, 17),  // ㅂ
                ['Q'] = JamoKey.Consonant(8, 0),   // ㅃ
                ['t'] = JamoKey.Consonant(9, 19),  // ㅅ
                ['T'] = JamoKey.Consonant(10, 20), // ㅆ
                ['d'] = JamoKey.Consonant(11, 21), // ㅇ
                ['w'] = JamoKey.Consonant(12, 22), // ㅈ
                ['W'] = JamoKey.Consonant(13, 0),  // ㅉ
                ['c'] = JamoKey.Consonant(14, 23), // ㅊ
                ['z'] = JamoKey.Consonant(15, 24), // ㅋ
                ['x'] = JamoKey.Consonant(16, 25), // ㅌ
                ['v'] = JamoKey.Consonant(17, 26), // ㅍ
                ['g'] = JamoKey.Consonant(18, 27), // ㅎ

                ['k'] = JamoKey.Vowel(0),  // ㅏ
                ['o'] = JamoKey.Vowel(1),  // ㅐ
                ['i'] = JamoKey.Vowel(2),  // ㅑ
                ['O'] = JamoKey.Vowel(3),  // ㅒ
                ['j'] = JamoKey.Vowel(4),  // ㅓ
                ['p'] = JamoKey.Vowel(5),  // ㅔ
                ['u'] = JamoKey.Vowel(6),  // ㅕ
                ['P'] = JamoKey.Vowel(7),  // ㅖ
                ['h'] = JamoKey.Vowel(8),  // ㅗ
                ['y'] = JamoKey.Vowel(12), // ㅛ
                ['n'] = JamoKey.Vowel(13), // ㅜ
                ['b'] = JamoKey.Vowel(17), // ㅠ
                ['m'] = JamoKey.Vowel(18), // ㅡ
                ['l'] = JamoKey.Vowel(20), // ㅣ
            };
        }

        readonly struct JamoKey
        {
            public readonly int initial;
            public readonly int medial;
            public readonly int final;
            public bool isVowel => medial >= 0;

            JamoKey(int initial, int medial, int final)
            {
                this.initial = initial;
                this.medial = medial;
                this.final = final;
            }

            public static JamoKey Consonant(int initial, int final)
            {
                return new JamoKey(initial, -1, final);
            }

            public static JamoKey Vowel(int medial)
            {
                return new JamoKey(-1, medial, 0);
            }
        }
    }
}
