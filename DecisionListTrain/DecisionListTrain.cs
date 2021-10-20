/*
 * Sentiment Analysis with Decision Lists: Training
 * Created by Kristian Vatsaas
 * October 2021
 * 
 * One application of language models is sentiment analysis, in which we try to classify a given text in some terms.
 * In this case, we want a system that can determine, with some level of accuracy, whether a given movie review
 * is negative or positive. This file implements the training part of the process. For this process, we are using
 * a decision list - a paired list of features (in this case, unigrams and bigrams) and the class they imply, sorted
 * by the strength with which they imply their class (known as log-likelihood). We also include 'not handling,'
 * which means that we prepend 'NOT_' to each token following the word 'not' (or n't) until end punctuation is reached.
 * This program is followed by DecisionListTest.cs and DecisionListEval.cs, and also depends on Decision.cs.
 * 
 * The decision list, once created, is output to a file, to be read in for the testing step. Each line represents a
 * single decision, including the feature, its log-likelihood, and its class.  The computation of log-likelihood is
 * included in Decision.cs. An example of a few lines:
 * 
 * seagal                                     5.7549    0
 * jackie brown                               5.4594    1
 * pulp fiction                               5.3923    1
 * mulan                                      5.2854    1
 *  
 *  This implementation uses the below algorithm.
 *  1. Open the input file using StreamReader.
 *      a. Get the next line of the file, which is a full review including its header.
 *      b. Store the coded class (1 or 0, representing positive or negative) and the text of the review, and discard
 *          the rest of the header.
 *      c. Split the review into sentences, and for each sentence:
 *          i. Split the text into individual tokens (words and punctuation).
 *          ii. Remove ignored tokens (those devoid of semantic value - more on this below)
 *          iii. Add not handling if necessary.
 *          iv. Build unigrams and bigrams from the tokens, then add them to the list of decisions.**
 *      d. Return to step a if end of file has not been reached.
 *  2. Calculate absolute log-likelihoods for each decision.
 *  3. Sort the list of decisions by likelihood (descending).
 *  4. Output the list of decisions to the output file.
 * 
 * **This file includes three different methods of counting features (frequence, presence, and a hybrid method), so we
 * will cover each method in the source code below.
 * 
 * This program was created for CS 4242 Natural Language Processing at the University of Minnesota-Duluth.
 * Last updated 10/19/21
 */

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
        /* This is a list of tokens that are excluded from the ngrams for the purpose of the decision list. While
         * not necessarily devoid of semantic value, when working with bigrams, these tokens are more likely
         * to water down the ngram (i.e. make it less unique) while potentially blocking the formation of a useful
         * one. For example, there are many phrases of the form "X and Y" where "X Y" would retain most or all
         * of the semantic value, but "X and" or "and Y" would lose significant semantic value.
         * 
         * This object is created separately in DecisionListTest.cs so that the two work separately - I previously
         * had a separate class with some shared objects and help functions, but ended up not having enough
         * in there to justify its existence.
         */
        static List<string> IgnoredTokens = new List<string>() {
            "a", "an", "the", "to", "of", "and",
            ".", ",", "'", "\"", ";", ":", "-", "(", ")", "&" };
        
        // Specifies the maximum count for an ngram per review - see TrainHybrid for more detail.
        static readonly int hybridMax = 2; 

        /// <summary>
        /// Reads each review from the input file and trains the system on it, sentence by sentence.
        /// </summary>
        /// <param name="filepath">The path of the training file</param>
        /// <param name="mode">The training mode to use - f for frequency, p for presence, and h for hybrid</param>
        /// <returns>A dictionary with the feature as the key and a Decision object as the value</returns>
        static Dictionary<string, Decision> ParseTrainingFile(string filepath, char mode)
        {
            var features = new Dictionary<string, Decision>();
            try
            {
                using var sr = new StreamReader(filepath);

                // read and parse one review at a time - each is a single 'line' in the plaintext file
                while (sr.Peek() != -1) // not EOF
                {
                    var input = sr.ReadLine();  // get the next review

                    // find and store the predetermined class and the review text
                    var match = Regex.Match(input, @"^.*\.txt (?<class>[01]) (?<text>.*)", RegexOptions.Compiled);  
                    var positive = match.Groups["class"].Value.Equals("1");
                    var text = match.Groups["text"].Value;

                    // split the review into sentences
                    var sentences = Regex.Split(text, @"(?<=[\.\?\!])", RegexOptions.Compiled);

                    // call training method based on specified mode
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

        /// <summary>
        /// Performs training by counting every occurrence of a feature.
        /// </summary>
        /// <param name="features">The dictionary of features and decisions</param>
        /// <param name="sentences">The sentences in the review</param>
        /// <param name="positive">True if the review is known to be positive, false if negative</param>
        static void TrainFrequency(Dictionary<string, Decision> features, string[] sentences, bool positive)
        {
            foreach (string sentence in sentences)
            {
                var matches = Regex.Matches(sentence, @"[^ ]+", RegexOptions.Compiled);     // get each token
                var tokens = new List<string>();
                foreach (Match m in matches)    // add word to token list if not an ignored token
                    if (!IgnoredTokens.Contains(m.Value))
                        tokens.Add(m.Value);
                HandleNot(tokens);      // do 'not' handling
                BuildNGrams(tokens, features, 1, positive);     // add unigrams to feature list
                BuildNGrams(tokens, features, 2, positive);     // add bigrams to feature list
            }
        }

        /// <summary>
        /// Performs training by testing for presence in the given review, then incrementing the associated
        /// count in the dictionary by one. In other words, the count refers to the number of reviews
        /// in which the feature occurred, not the total number of appearances.
        /// </summary>
        /// <param name="features">The dictionary of features and decisions</param>
        /// <param name="sentences">The sentences in the review</param>
        /// <param name="positive">True if the review is known to be positive, false if negative</param>
        static void TrainPresence(Dictionary<string, Decision> features, string[] sentences, bool positive)
        {
            var reviewFeatures = new HashSet<string>();
            foreach (string sentence in sentences)
            {
                var matches = Regex.Matches(sentence, @"[^ ]+", RegexOptions.Compiled);     // get each token
                var tokens = new List<string>();
                foreach (Match m in matches)    // add word to token list if not an ignored token
                    if (!IgnoredTokens.Contains(m.Value))
                        tokens.Add(m.Value);
                HandleNot(tokens);      // do 'not' handling
                BuildNGrams(tokens, reviewFeatures, 1);     // add unigrams to review-scoped feature list
                BuildNGrams(tokens, reviewFeatures, 2);     // add bigrams to review-scoped feature list
            }

            foreach (string feature in reviewFeatures)
            {
                if (!features.ContainsKey(feature))             // add feature to full feature list if not already present
                    features.Add(feature, new Decision(feature));
                features[feature].IncrementCount(positive);     // increment counter for feature
            }
        }

        /// <summary>
        /// Performs training by counting at most [hybridMax] occurrences of a feature per review. The
        /// TrainPresence method can be thought of as TrainHybrid with a max of 1.
        /// </summary>
        /// <param name="features">The dictionary of features and decisions</param>
        /// <param name="sentences">The sentences in the review</param>
        /// <param name="positive">True if the review is known to be positive, false if negative</param>
        static void TrainHybrid(Dictionary<string, Decision> features, string[] sentences, bool positive)
        {
            var reviewFeatures = new Dictionary<string, Decision>();     // get each token
            foreach (string sentence in sentences)
            {
                var matches = Regex.Matches(sentence, @"[^ ]+", RegexOptions.Compiled);
                var tokens = new List<string>();
                foreach (Match m in matches)    // add word to token list if not an ignored token
                    if (!IgnoredTokens.Contains(m.Value))
                        tokens.Add(m.Value);
                HandleNot(tokens);      // do 'not' handling
                BuildNGrams(tokens, reviewFeatures, 1, positive, hybridMax);     // add unigrams to review-scoped feature list
                BuildNGrams(tokens, reviewFeatures, 2, positive, hybridMax);     // add bigrams to review-scoped feature list
            }

            foreach (Decision d in reviewFeatures.Values)
            {
                if (!features.ContainsKey(d.Feature))   // add feature to full feature list if not already present
                    features.Add(d.Feature, d);
                else
                    features[d.Feature].MergeCount(d);  // add local count to counter for feature
            }
        }

        /// <summary>
        /// This signature of BuildNGrams is for TrainFrequency. It creates and counts all possible ngrams, for
        /// the given 'n', and adds their count to the feature list.
        /// </summary>
        /// <param name="tokens">The list of tokens</param>
        /// <param name="features">The dictionary of features and decisions</param>
        /// <param name="n">The ngram size</param>
        /// <param name="positive">True if the review is known to be positive, false if negative</param>
        static void BuildNGrams(List<string> tokens, Dictionary<string, Decision> features, int n, bool positive)
        {
            for (int i = 0; i <= tokens.Count - n; i++)
            {
                var feature = tokens[i];
                // combine n tokens, separated by spaces, starting from i
                for (int j = 1; j < n; j++)
                    feature += " " + tokens[i + j];
                if (!features.ContainsKey(feature))         // add to feature list if not present
                    features.Add(feature, new Decision(feature));
                features[feature].IncrementCount(positive); // increment count
            }
        }

        /// <summary>
        /// This signature of BuildNGrams is for TrainPresence. It creates all possible ngrams, for the given
        /// 'n', and increments the feature list for the features present.
        /// </summary>
        /// <param name="tokens">The list of tokens</param>
        /// <param name="features">The dictionary of features and decisions</param>
        /// <param name="n">The ngram size</param>
        static void BuildNGrams(List<string> tokens, HashSet<string> features, int n)
        {
            for (int i = 0; i <= tokens.Count - n; i++)
            {
                var feature = tokens[i];
                // combine n tokens, separated by spaces, starting from i
                for (int j = 1; j < n; j++)
                    feature += " " + tokens[i + j];
                features.Add(feature);  // add to feature list if not present
            }
        }

        /// <summary>
        /// This signature of BuildNGrams is for TrainHybrid. It creates all possible ngrams, for the given
        /// 'n', and increments the related count in the feature list by at most the max.
        /// </summary>
        /// <param name="tokens">The list of tokens</param>
        /// <param name="features">The dictionary of features and decisions</param>
        /// <param name="n">The ngram size</param>
        /// <param name="positive">True if the review is known to be positive, false if negative</param>
        /// <param name="max">The maximum number of features to count</param>
        static void BuildNGrams(List<string> tokens, Dictionary<string, Decision> features, int n, bool positive, int max)
        {
            for (int i = 0; i <= tokens.Count - n; i++)
            {
                var feature = tokens[i];
                // combine n tokens, separated by spaces, starting from i
                for (int j = 1; j < n; j++)
                    feature += " " + tokens[i + j];
                if (!features.ContainsKey(feature))     // add to feature list if not present, then increment count
                {
                    features.Add(feature, new Decision(feature));
                    features[feature].IncrementCount(positive);
                }
                else if (features[feature].GetCount(positive) < max)    // if present, increment count
                {
                    features[feature].IncrementCount(positive);
                }
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
        /// Use the built-in method for Decision to calculate the log-likelihood for every decision in the dictionary.
        /// Should not be used until counting is done.
        /// </summary>
        /// <param name="features">The dictionary of features and decisions</param>
        /// <returns>The list of decisions</returns>
        static List<Decision> CalculateLogLikelihoods(Dictionary<string, Decision> features)
        {
            var decisions = features.Values.ToList();
            foreach (Decision d in features.Values)
                d.CalculateLogLikelihood();
            return decisions;
        }

        /// <summary>
        /// Manages method calls for training and writes the results to the output file.
        /// </summary>
        /// <param name="args">The first argument is the filepath for the training set of reviews,
        /// and the second is the filepath for the decision list to be output.</param>
        static void Main(string[] args)
        {
            // check number of arguments
            if (args.Length != 2)
            {
                Console.WriteLine("Incorrect number of arguments!");
                return;
            }

            // assign filepaths for readability
            var trainFile = args[0];
            var outfile = args[1];

            /* Parse the file and count features. Currently, this is using the presence implementation, which has 
             * proven to be the most successful in testing to this point. */
            var features = ParseTrainingFile(trainFile, 'p');

            // calculate log likelihoods
            var decisions = CalculateLogLikelihoods(features);

            // sort (descending) by likelihood
            decisions.Sort(Decision.CompareByLikelihood);

            try
            {
                using var sw = new StreamWriter(outfile);

                // threshold set here as testing does not get close to passing this point in the decision list for any method
                var threshold = 2.5;

                // write each decision with a log-likelihood above the threshold to the output file
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
