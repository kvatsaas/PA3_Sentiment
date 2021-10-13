using Sentiment;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace DecisionListTest
{
    class DecisionListTest
    {

        static List<string> IgnoredTokens = new List<string>() { 
            "a", "an", "the", "to", "of", "and",
            ".", ",", "'", "\"", ";", ":", "-", "(", ")", "&" };

        static List<Decision> ParseDecisionList(string filepath)
        {
            var decisions = new List<Decision>();

            try
            {
                using var sr = new StreamReader(filepath);
                // read and parse one decision at a time - each is a single line in the plaintext file
                while (sr.Peek() != -1) // not EOF
                {
                    var input = sr.ReadLine();

                    var match = Regex.Match(input, @"(?<feature>.*)\b\s+(?<log>\d*\.\d{4})\s+(?<class>[01])", RegexOptions.Compiled);
                    decisions.Add(
                        new Decision(
                            match.Groups["feature"].Value,
                            match.Groups["class"].Value.Equals("1")));
                }
            }
            catch (Exception e)
            {
                throw new Exception("Something went wrong while reading " + filepath + ": " + e.Message);
            }

            return decisions;
        }

        static Dictionary<string,string> ParseTestFile(string filepath)
        {
            var reviews = new Dictionary<string, string>();
            try
            {
                using var sr = new StreamReader(filepath);
                // read and parse one review at a time - each is a single 'line' in the plaintext file
                while (sr.Peek() != -1) // not EOF
                {
                    var input = sr.ReadLine();

                    var match = Regex.Match(input, @"^(?<id>.*\.txt) __ (?<text>.*)", RegexOptions.Compiled);
                    var id = match.Groups["id"].Value;
                    var text = match.Groups["text"].Value;

                    var sentences = Regex.Split(text, @"(?<=[\.\?\!])", RegexOptions.Compiled);
                    var parsedReview = "";
                    foreach (string sentence in sentences)
                    {
                        var matches = Regex.Matches(sentence, @"[^ ]+", RegexOptions.Compiled);
                        var tokens = new List<string>();
                        foreach (Match m in matches)
                            if (!IgnoredTokens.Contains(m.Value))
                                tokens.Add(m.Value);
                        HandleNot(tokens);
                        parsedReview += String.Join(" ", tokens) + " ";
                    }
                    reviews.Add(id, parsedReview);
                }
                return reviews;
            }
            catch (Exception e)
            {
                throw new Exception("Something went wrong while reading " + filepath + ": " + e.Message);
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

        static Dictionary<string, bool> ClassifyAllReviews(List<Decision> decisions, Dictionary<string, string> reviews)
        {
            var classifiedReviews = new Dictionary<string, bool>();

            foreach (string id in reviews.Keys)
                classifiedReviews.Add(id, ClassifyReview(decisions, reviews[id]));

            return classifiedReviews;
        }

        static bool ClassifyReview(List<Decision> decisions, string review)
        {
            foreach (Decision d in decisions)
                if (review.Contains(" " + d.Feature + " "))
                    return d.Classification;
            return true;
        }

        static void Main(string[] args)
        {
            // check number of arguments
            if (args.Length != 3)
            {
                Console.WriteLine("Incorrect number of arguments!");
                return;
            }

            var decisionFile = args[0];
            var testFile = args[1];
            var outfile = args[2];

            var decisions = ParseDecisionList(decisionFile);
            var reviews = ParseTestFile(testFile);
            var classifiedReviews = ClassifyAllReviews(decisions, reviews);

            try
            {
                using var sw = new StreamWriter(outfile);
                foreach (string id in classifiedReviews.Keys)
                    sw.WriteLine("{0} {1}", id, classifiedReviews[id] ? 1 : 0);
            }
            catch (Exception e)
            {
                throw new Exception("Something went wrong while writing to " + outfile + ": " + e.Message);
            }
        }
    }
}
