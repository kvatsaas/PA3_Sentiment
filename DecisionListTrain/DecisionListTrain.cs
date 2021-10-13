using Sentiment;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace DecisionListTrain
{

    class DecisionListTrain
    {
        static List<string> IgnoredTokens = new List<string>() {
            "a", "an", "the", "to", "of", "and",
            ".", ",", "'", "\"", ";", ":", "-", "(", ")", "&" };
        
        static readonly int hybridMax = 2; 

        static Dictionary<string, Decision> ParseTrainingFile(string filepath, char mode)
        {
            var features = new Dictionary<string, Decision>();
            try
            {
                using var sr = new StreamReader(filepath);
                // read and parse one review at a time - each is a single 'line' in the plaintext file
                while (sr.Peek() != -1) // not EOF
                {
                    var input = sr.ReadLine();

                    var match = Regex.Match(input, @"^.*\.txt (?<type>[01]) (?<text>.*)", RegexOptions.Compiled);
                    var positive = match.Groups["type"].Value.Equals("1");
                    var text = match.Groups["text"].Value;

                    var sentences = Regex.Split(text, @"(?<=[\.\?\!])", RegexOptions.Compiled);

                    switch (mode)
                    {
                        case 'f':
                            TrainFrequency(features, sentences, positive);
                            break;

                        case 'p':
                            TrainPresence(features, sentences, positive);
                            break;

                        case 'h':
                            TrainHybrid(features, sentences, positive);
                            break;

                        default:
                            throw new Exception("Ivalid parse mode: " + mode);
                    }
                }

                return features;
            }
            catch (Exception e)
            {
                throw new Exception("Something went wrong while reading " + filepath + ": " + e.Message);
            }
        }

        static void TrainFrequency(Dictionary<string, Decision> features, string[] sentences, bool positive)
        {
            foreach (string sentence in sentences)
            {
                var matches = Regex.Matches(sentence, @"[^ ]+", RegexOptions.Compiled);
                var tokens = new List<string>();
                foreach (Match m in matches)
                    if (!IgnoredTokens.Contains(m.Value))
                        tokens.Add(m.Value);
                HandleNot(tokens);
                BuildNGrams(tokens, features, 1, positive);     // add unigrams to feature list
                BuildNGrams(tokens, features, 2, positive);     // add bigrams to feature list
            }
        }

        static void TrainPresence(Dictionary<string, Decision> features, string[] sentences, bool positive)
        {
            var reviewFeatures = new HashSet<string>();
            foreach (string sentence in sentences)
            {
                var matches = Regex.Matches(sentence, @"[^ ]+", RegexOptions.Compiled);
                var tokens = new List<string>();
                foreach (Match m in matches)
                    if (!IgnoredTokens.Contains(m.Value))
                        tokens.Add(m.Value);
                HandleNot(tokens);
                BuildNGrams(tokens, reviewFeatures, 1);     // add unigrams to feature list
                BuildNGrams(tokens, reviewFeatures, 2);     // add bigrams to feature list
            }

            foreach (string feature in reviewFeatures)
            {
                if (!features.ContainsKey(feature))
                    features.Add(feature, new Decision(feature));
                features[feature].IncrementCount(positive);
            }
        }

        static void TrainHybrid(Dictionary<string, Decision> features, string[] sentences, bool positive)
        {
            var reviewFeatures = new Dictionary<string, Decision>();
            foreach (string sentence in sentences)
            {
                var matches = Regex.Matches(sentence, @"[^ ]+", RegexOptions.Compiled);
                var tokens = new List<string>();
                foreach (Match m in matches)
                    if (!IgnoredTokens.Contains(m.Value))
                        tokens.Add(m.Value);
                HandleNot(tokens);
                BuildNGrams(tokens, reviewFeatures, 1, positive, hybridMax);     // add unigrams to feature list
                BuildNGrams(tokens, reviewFeatures, 2, positive, hybridMax);     // add bigrams to feature list
            }

            foreach (Decision d in reviewFeatures.Values)
            {
                if (!features.ContainsKey(d.Feature))
                    features.Add(d.Feature, d);
                else
                    features[d.Feature].MergeCount(d);
            }
        }

        static void BuildNGrams(List<string> tokens, Dictionary<string, Decision> features, int n, bool positive)
        {
            for (int i = 0; i <= tokens.Count - n; i++)
            {
                var feature = tokens[i];
                for (int j = 1; j < n; j++)
                    feature += " " + tokens[i + j];
                if (!features.ContainsKey(feature))
                    features.Add(feature, new Decision(feature));
                features[feature].IncrementCount(positive);
            }
        }

        static void BuildNGrams(List<string> tokens, Dictionary<string, Decision> features, int n, bool positive, int max)
        {
            for (int i = 0; i <= tokens.Count - n; i++)
            {
                var feature = tokens[i];
                for (int j = 1; j < n; j++)
                    feature += " " + tokens[i + j];
                if (!features.ContainsKey(feature))
                {
                    features.Add(feature, new Decision(feature));
                    features[feature].IncrementCount(positive);
                }
                else if (features[feature].GetCount(positive) < max)
                {
                    features[feature].IncrementCount(positive);
                }
            }
        }

        static void BuildNGrams(List<string> tokens, HashSet<string> features, int n)
        {
            for (int i = 0; i <= tokens.Count - n; i++)
            {
                var feature = tokens[i];
                for (int j = 1; j < n; j++)
                    feature += " " + tokens[i + j];
                features.Add(feature);
            }
        }

        public static void HandleNot(List<string> tokens)
        {
            for (int i = 0; i < tokens.Count - 1; i++)
            {
                if (tokens[i].Equals("not") || tokens[i].EndsWith("n't"))
                {
                    for (int j = i + 1; j < tokens.Count; j++)
                        tokens[j] = "NOT_" + tokens[j];
                    return;
                }
            }
        }

        static List<Decision> CalculateLogLikelihoods(Dictionary<string, Decision> features)
        {
            var decisions = features.Values.ToList();
            foreach (Decision d in features.Values)
                d.CalculateLogLikelihood();
            return decisions;
        }

        static void Main(string[] args)
        {
            // check number of arguments
            if (args.Length != 2)
            {
                Console.WriteLine("Incorrect number of arguments!");
                return;
            }

            var trainFile = args[0];
            var outfile = args[1];
            var features = ParseTrainingFile(trainFile, 'f');
            var decisions = CalculateLogLikelihoods(features);
            decisions.Sort(Decision.CompareByLikelihood);

            try
            {
                using var sw = new StreamWriter(outfile);
                var threshold = Decision.UseComplexSmoothing ? 1 : 1.0001;

                foreach (Decision decision in decisions)
                {
                    if (decision.LogLikelihood < threshold)
                        break;
                    sw.WriteLine("{0,-40} {1,8:N4} {2,4}",
                        decision.Feature,
                        Math.Round(decision.LogLikelihood, 4),
                        decision.Classification ? 1 : 0);
                }
            }
            catch (Exception e)
            {
                throw new Exception("Something went wrong while writing to " + outfile + ": " + e.Message);
            }
        }
    }
}
