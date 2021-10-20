/*
 * Sentiment Analysis with Decision Lists: Training
 * Created by Kristian Vatsaas
 * October 2021
 * 
 * One application of language models is sentiment analysis, in which we try to classify a given text in some terms.
 * In this case, we want a system that can determine, with some level of accuracy, whether a given movie review
 * is negative or positive. This file implements the testing part of the process. For this process, we read in the
 * decision list created beforehand and use it to classify given reviews of unknown class.
 * This program is preceded by DecisionListTrain.cs and followed by DecisionListEval.cs, and also depends on Decision.cs.
 * 
 * Each classification, once determined, is output to a text file along with the review id, which looks like the following:
 * 
 * cv666_tok-13320.txt 1
 * cv535_tok-19937.txt 0
 * cv245_tok-19462.txt 0
 *  
 *  This implementation uses the below algorithm.
 *  1. Open the decision list file using StreamReader.
 *      a. Get the next line of the file, which is a single decision.
 *      b. Store the feature and class of the decision.
 *      c. Return to step a if end of file has not been reached.
 *  2. Open the reviews file using StreamReader.
 *      a. Get the next line of the file, which is a single decision.
 *      b. Store the id and text of the review.
 *      c. Split the review into sentences, and for each sentence:
 *          i. Split the text into individual tokens (words and punctuation).
 *          ii. Remove ignored tokens (those devoid of semantic value - more on this below)
 *          iii. Add not handling if necessary.
 *          iv. Add the modified tokens to a string representing the review after not handling.
 *  3. For each review, iterate through the decision list one at a time; once a feature is found in the review text,
 *      store the review id and the respective classifier in a list.
 *  4. Output the list of review ids and their classifiers to the output file.
 * 
 * This program was created for CS 4242 Natural Language Processing at the University of Minnesota-Duluth.
 * Last updated 10/19/21
 */

using Sentiment;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace DecisionListTest
{
    class DecisionListTest
    {
        /* This is a list of tokens that are excluded from the ngrams for the purpose of the decision list. While
         * not necessarily devoid of semantic value, when working with bigrams, these tokens are more likely
         * to water down the ngram (i.e. make it less unique) while potentially blocking the formation of a useful
         * one. For example, there are many phrases of the form "X and Y" where "X Y" would retain most or all
         * of the semantic value, but "X and" or "and Y" would lose significant semantic value.
         * 
         * This object is created separately in DecisionListTrain.cs so that the two work separately - I previously
         * had a separate class with some shared objects and help functions, but ended up not having enough
         * in there to justify its existence.
         */
        static List<string> IgnoredTokens = new List<string>() { 
            "a", "an", "the", "to", "of", "and",
            ".", ",", "'", "\"", ";", ":", "-", "(", ")", "&" };

        /// <summary>
        /// Builds a decision list from the given input file. Assumes proper formatting of the input file.
        /// </summary>
        /// <param name="filepath">The path of the decision list file</param>
        /// <returns>The completed decision list</returns>
        static List<Decision> ParseDecisionList(string filepath)
        {
            var decisions = new List<Decision>();

            try
            {
                using var sr = new StreamReader(filepath);
                // read and parse one decision at a time - each is a single line in the plaintext file
                while (sr.Peek() != -1) // not EOF
                {
                    // read next decision
                    var input = sr.ReadLine();

                    // extract the feature, log-likelihood, and associated class
                    var match = Regex.Match(input, @"(?<feature>.*)\b\s+(?<log>\d*\.\d{4})\s+(?<class>[01])", RegexOptions.Compiled);

                    // create the decision with its feature and class, and add it to the list. since it is already sorted, LL is not used
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

        /// <summary>
        /// Reads each review from the given input file and prepares it for classification by removing ignored tokens and doing 'not'
        /// handling. Assumes proper formatting of the input file.
        /// </summary>
        /// <param name="filepath">The filepath of the review file</param>
        /// <returns>A dictionary of the review ID mapped to the modified review text.</returns>
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

                    // find and store the review ID and the text
                    var match = Regex.Match(input, @"^(?<id>.*\.txt) __ (?<text>.*)", RegexOptions.Compiled);
                    var id = match.Groups["id"].Value;
                    var text = match.Groups["text"].Value;

                    // split the review into sentences
                    var sentences = Regex.Split(text, @"(?<=[\.\?\!])", RegexOptions.Compiled);
                    var parsedReview = "";
                    foreach (string sentence in sentences)
                    {
                        var matches = Regex.Matches(sentence, @"[^ ]+", RegexOptions.Compiled); // split into tokens
                        var tokens = new List<string>();
                        foreach (Match m in matches)    // add word to token list if not an ignored token
                            if (!IgnoredTokens.Contains(m.Value))
                                tokens.Add(m.Value);
                        HandleNot(tokens);      // do 'not' handling
                        // rebuild the review as a single string - extra spaces enabler a simpler check during testing
                        parsedReview += String.Join(" ", tokens) + " ";
                    }
                    reviews.Add(id, parsedReview);  // add to the review dictionary
                }
                return reviews;
            }
            catch (Exception e)
            {
                throw new Exception("Something went wrong while reading " + filepath + ": " + e.Message);
            }
        }

        /// <summary>
        /// If the word 'not' or the substring "n't" is among the tokens, prepend 'NOT_' to every token after it.
        /// This method is created separately in DecisionListTrain.cs so that the two work separately - I previously
        /// had a separate class with some shared objects and help functions, but ended up not having enough
        /// in there to justify its existence.
        /// </summary>
        /// <param name="tokens">The list of tokens</param>
        public static void HandleNot(List<string> tokens)
        {
            for (int i = 0; i < tokens.Count - 1; i++)
            {
                if (tokens[i].Equals("not") || tokens[i].EndsWith("n't"))
                {
                    for (int j = i + 1; j < tokens.Count; j++)  // prepend 'NOT_' to every word in the rest of the sentence
                        tokens[j] = "NOT_" + tokens[j];
                    return;     // exit loop once one instance has been encountered
                }
            }
        }

        /// <summary>
        /// Classifies each review in the dictionary according to the decision list.
        /// </summary>
        /// <param name="decisions">The list of decisions</param>
        /// <param name="reviews">The dictionary of reviews</param>
        /// <returns>A dictionary containing each review ID and its classification</returns>
        static Dictionary<string, bool> ClassifyAllReviews(List<Decision> decisions, Dictionary<string, string> reviews)
        {
            var classifiedReviews = new Dictionary<string, bool>();

            foreach (string id in reviews.Keys)
                classifiedReviews.Add(id, ClassifyReview(decisions, reviews[id]));  // classify the review and add it to the dictionary

            return classifiedReviews;
        }

        /// <summary>
        /// Classifies the given review according to the decision list. This is separate from ClassifyAllReviews so that it's easier
        /// to break properly.
        /// </summary>
        /// <param name="decisions">The list of decisions</param>
        /// <param name="review">The review</param>
        /// <returns>True if classified as a positive review, otherwise false</returns>
        static bool ClassifyReview(List<Decision> decisions, string review)
        {
            // check for presence of each feature and, if present, return its associated classification
            foreach (Decision d in decisions)
                if (review.Contains(" " + d.Feature + " "))
                    return d.Classification;
            // if none are found, return false - a default chosen because I personally don't watch a lot of movies, and really need to be convinced!
            return false;
        }

        /// <summary>
        /// Manages method calls for testing and outputs the results.
        /// </summary>
        /// <param name="args">The first argument is the filepath for the decision list, the second is the filepath for the reviews
        /// to be tested, and the third is the filepath for the classifications.</param>
        static void Main(string[] args)
        {
            // check number of arguments
            if (args.Length != 3)
            {
                Console.WriteLine("Incorrect number of arguments!");
                return;
            }

            // assign filepaths for readability
            var decisionFile = args[0];
            var testFile = args[1];
            var outfile = args[2];

            var decisions = ParseDecisionList(decisionFile);    // build decision list
            var reviews = ParseTestFile(testFile);              // prepare reviews for testing
            var classifiedReviews = ClassifyAllReviews(decisions, reviews);     // test reviews

            try
            {
                using var sw = new StreamWriter(outfile);

                // print each review id and its classification to the output file
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
