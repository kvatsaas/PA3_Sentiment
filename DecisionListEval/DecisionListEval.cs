using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace DecisionListEval
{


    class DecisionListEval
    {

        static Dictionary<string, bool> ParseRatingsFile(string filepath)
        {
            var ratings = new Dictionary<string, bool>();

            try
            {
                using var sr = new StreamReader(filepath);
                // read and parse one decision at a time - each is a single line in the plaintext file
                while (sr.Peek() != -1) // not EOF
                {
                    var input = sr.ReadLine();

                    var match = Regex.Match(input, @"(?<id>.*)\b\s+(?<class>[01])", RegexOptions.Compiled);
                    ratings.Add(match.Groups["id"].Value, match.Groups["class"].Value.Equals("1"));
                }
            }
            catch (Exception e)
            {
                throw new Exception("Something went wrong while reading " + filepath + ": " + e.Message);
            }

            return ratings;
        }

        static void Main(string[] args)
        {
            // check number of arguments
            if (args.Length != 3)
            {
                Console.WriteLine("Incorrect number of arguments!");
                return;
            }

            var goldFile = args[0];
            var systemFile = args[1];
            var outfile = args[2];

            var goldRatings = ParseRatingsFile(goldFile);
            var systemRatings = ParseRatingsFile(systemFile);

            double tp, fp, fn, tn;
            tp = fp = fn = tn = 0;

            try
            {
                using var sw = new StreamWriter(outfile);



                foreach (string id in goldRatings.Keys)
                {
                    sw.WriteLine("{0} {1} {2}", id, goldRatings[id] ? 1 : 0, systemRatings[id] ? 1 : 0);
                    if (systemRatings[id])
                    {
                        if (goldRatings[id])
                            tp++;
                        else
                            fp++;
                    }
                    else
                    {
                        if (goldRatings[id])
                            fn++;
                        else
                            tn++;
                    }
                }

                sw.WriteLine("Accuracy: {0:N4}", Math.Round((tp + tn) / (tp + fp + tn + fn), 4));
                sw.WriteLine("Precision: {0:N4}", Math.Round(tp / (tp + fp), 4));
                sw.WriteLine("Recall: {0:N4}", Math.Round(tp / (tp + fn), 4));
            }
            catch (Exception e)
            {
                throw new Exception("Something went wrong while writing to " + outfile + ": " + e.Message);
            }

        }
    }
}
