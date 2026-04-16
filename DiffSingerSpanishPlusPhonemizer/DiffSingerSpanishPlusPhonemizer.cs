using System;
using System.Collections.Generic;
using System.Linq;
using OpenUtau.Api;
using OpenUtau.Core.G2p;

namespace OpenUtau.Core.DiffSinger
{
    [Phonemizer("DiffSinger Spanish+ Phonemizer", "DIFFS ES+", language: "ES")]
    public class DiffSingerSpanishPlusPhonemizer : DiffSingerG2pPhonemizer
    {
        protected override string GetDictionaryName()=>"dsdict-es.yaml";
        public override string GetLangCode()=>"es";
        protected override IG2p LoadBaseG2p() => new SpanishG2p();
        protected override string[] GetBaseG2pVowels() => new string[] {
            "a", "e", "i", "o", "u"
        };

        protected override string[] GetBaseG2pConsonants() => new string[] {
            "b", "B", "ch", "d", "D", "f", "g", "G", "gn", "I", "k", "l",
            "ll", "m", "n", "p", "r", "rr", "s", "t", "U", "w", "x", "y", "Y", "z"
        };

        public override Result Process(Note[] notes, Note? prev, Note? next, Note? prevNeighbour, Note? nextNeighbour, Note[] prevs) {
            if (notes[0].lyric == "-") {
                return MakeSimpleResult("SP");
            }
            if (notes[0].lyric == "br") {
                return MakeSimpleResult("AP");
            }
            if (!partResult.TryGetValue(notes[0].position, out var phonemes)) {
                throw new Exception("Result not found in the part");
            }
            var processedPhonemes = new List<Phoneme>();
            var langCode = GetLangCode() + "/";

            string prevPhoneme = "";
            // Apply the rule if this is a main note (not a + note) and there's a previous note in the phrase
            // This includes cases where + notes extend the previous note's duration
            if (prevNeighbour.HasValue && !notes[0].lyric.StartsWith("+")) {
                prevPhoneme = GetPreviousPhoneme(notes[0].position);
            }

            for (int i = 0; i < phonemes.Count; i++) {
                var tu = phonemes[i];
                string phoneme = tu.Item1;

                if (ShouldReplacePhoneme(phoneme, prevPhoneme)) {
                    phoneme = GetReplacementPhoneme(phoneme);
                }

                processedPhonemes.Add(new Phoneme() {
                    phoneme = phoneme,
                    position = tu.Item2
                });

                prevPhoneme = phoneme;
            }
            return new Result {
                phonemes = processedPhonemes.ToArray()
            };
        }

        private string GetPreviousPhoneme(int currentPos) {
            var prevs = partResult.Where(kv => kv.Key < currentPos).OrderByDescending(kv => kv.Key);
            foreach (var kv in prevs) {
                if (kv.Value.Count > 0) {
                    return kv.Value.Last().Item1;
                }
            }
            return "";
        }

        private bool ShouldReplacePhoneme(string phoneme, string prevPhoneme) {
            var langCode = GetLangCode() + "/";
            var baseVowels = GetBaseG2pVowels().Concat(new[] { "I", "U", "w", "y" }).ToHashSet();
            string cleanPrev = prevPhoneme.Replace(langCode, "");
            bool prevIsVowelOrIU = baseVowels.Contains(cleanPrev);

            if (prevIsVowelOrIU) {
                if (phoneme == langCode + "g" || phoneme == "g") {
                    return true;
                }
                if (phoneme == langCode + "d" || phoneme == "d") {
                    return true;
                }
                if (phoneme == langCode + "b" || phoneme == "b") {
                    return true;
                }
            }
            return false;
        }

        private string GetReplacementPhoneme(string phoneme) {
            var langCode = GetLangCode() + "/";
            if (phoneme == langCode + "g" || phoneme == "g") {
                return HasPhoneme(langCode + "G") ? langCode + "G" : "G";
            }
            if (phoneme == langCode + "d" || phoneme == "d") {
                return HasPhoneme(langCode + "D") ? langCode + "D" : "D";
            }
            if (phoneme == langCode + "b" || phoneme == "b") {
                return HasPhoneme(langCode + "B") ? langCode + "B" : "B";
            }
            return phoneme;
        }
    }
}