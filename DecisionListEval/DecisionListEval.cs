/*
 * Sentiment Analysis with Decision Lists: Training
 * Created by Kristian Vatsaas
 * October 2021
 * 
 * One application of language models is sentiment analysis, in which we try to classify a given text in some terms.
 * In this case, we want a system that can determine, with some level of accuracy, whether a given movie review
 * is negative or positive. This file implements self-evaluation, in which the program compares the results from the
 * previous step with the gold standard (the correct answers).
 * This program is preceded by DecisionListTest.cs and DecisionListEval.cs.
 * 
 * The program outputs the classifications given by both the gold standard and itself, in that order, along with the
 * review id. It looks like the following:
 * 
 * cv666_tok-13320.txt 1 1
 * cv535_tok-19937.txt 0 0
 * cv245_tok-19462.txt 1 0
 * 
 * It also outputs the accuracy, precision, and recall scores, which looks like the following:
 * 
 * Accuracy: 0.7250
 * Precision: 0.7368
 * Recall: 0.7000
 *  
 *  This implementation uses the below algorithm.
 *  1. Open the gold standard file using StreamReader.
 *      a. Get the next line of the file, which is a single review classification.
 *      b. Store the id and class of the review.
 *      c. Return to step a if end of file has not been reached.
 *  2. Repeat step 1 for the system answers file.
 *  3. For each review id:
 *      a. Output the review id, the gold standard classification, and the system classification.
 *      b. Compare each classification and increment one of true positives, false positives,
 *          false negatives, and true negatives according to the following:
 *          - If both are positive, it is a true positive.
 *          - If the system classification is positive and the gold standard is negative,
 *              it is a false positive.
 *          - If the system classification is negative and the gold standard is positive,
 *              it is a false negative.
 *          - If both are negative, it is a true negative.
 *  4. Compute and output the accuracy, precision, and recall scores.**
 *  
 *  **Accuracy refers to total correct answers, precision is the proportion of actual positives
 *    among positive answers by the system, and recall is the proportion of positives detected
 *    from the pool of all positives.
 * 
 * This program was created for CS 4242 Natural Language Processing at the University of Minnesota-Duluth.
 * Last updated 10/19/21
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace DecisionListEval
{

    class DecisionListEval
    {

        /// <summary>
        /// Builds a classification list from the given input file. Assumes proper formatting of the input file.
        /// </summary>
        /// <param name="filepath">The path of the testing results file</param>
        /// <returns>A dictionary of review IDs and their classifications</returns>
        static Dictionary<string, bool> ParseClassificationFile(string filepath)
        {
            var classifications = new Dictionary<string, bool>();

            try
            {
                using var sr = new StreamReader(filepath);
                // read and parse one decision at a time - each is a single line in the plaintext file
                while (sr.Peek() != -1) // not EOF
                {
                    // read next classification
                    var input = sr.ReadLine();

                    // extract review ID and its classification, then add it to the dictionary
                    var match = Regex.Match(input, @"(?<id>.*)\b\s+(?<class>[01])", RegexOptions.Compiled);
                    classifications.Add(match.Groups["id"].Value, match.Groups["class"].Value.Equals("1"));
                }
            }
            catch (Exception e)
            {
                throw new Exception("Something went wrong while reading " + filepath + ": " + e.Message);
            }

            return classifications;
        }

        static void Main(string[] args)
        {
            // check number of arguments
            if (args.Length != 3)
            {
                Console.WriteLine("Incorrect number of arguments!");
                return;
            }

            // assign filepaths for readability
            var goldFile = args[0];
            var systemFile = args[1];
            var outfile = args[2];

            // create classification lists for the gold standard and the system's classifications
            var goldClassifications = ParseClassificationFile(goldFile);
            var systemClassifications = ParseClassificationFile(systemFile);

            // variables for true and false positives and negatives
            double tp, fp, fn, tn;
            tp = fp = fn = tn = 0;

            try
            {
                using var sw = new StreamWriter(outfile);
                // iterate through review IDs
                foreach (string id in goldClassifications.Keys)
                {
                    // write review ID and each classification to output file
                    sw.WriteLine("{0} {1} {2}", id, goldClassifications[id] ? 1 : 0, systemClassifications[id] ? 1 : 0);

                    // increment count for true/false positives/negatives
                    if (systemClassifications[id])
                    {
                        if (goldClassifications[id])
                            tp++;
                        else
                            fp++;
                    }
                    else
                    {
                        if (goldClassifications[id])
                            fn++;
                        else
                            tn++;
                    }
                }

                // output accuracy, precision, and recall
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
