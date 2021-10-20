/*
 * Sentiment Analysis with Decision Lists: Training
 * Created by Kristian Vatsaas
 * October 2021
 * 
 * This program was created for CS 4242 Natural Language Processing at the University of Minnesota-Duluth.
 * Last updated 10/19/21
 */

using System;

namespace Sentiment
{
    /// <summary>
    /// This class is used to keep track of features and their counts, and calculate their log-likelihood.
    /// Alternatively, it can be used for features that do not need any calculation, and simply have an
    /// associated classification.
    /// </summary>
    public class Decision
    {
        /* Determines whether to use a more complex form of smoothing for log-likelihood calculation.
         * Currently off, as the algorithm I came up with was overall worse than the simple version I used.*/
        static public readonly bool UseComplexSmoothing = false;

        /// <summary>
        /// This constructor is intended for features that need to have their counts tracked so that their
        /// log-likelihood and associated classification can later be calculated. This is the version used 
        /// by DecisionListTrain.cs.
        /// </summary>
        /// <param name="feature">The feauture this decision is for</param>
        public Decision(string feature)
        {
            Feature = feature;
            PositiveCount = 0.0;
            NegativeCount = 0.0;
        }

        /// <summary>
        /// This constructor is intended for decisions that already have classifications. This is the version
        /// used by DecisionListTest.cs.
        /// </summary>
        /// <param name="feature">The feauture this decision is for</param>
        /// <param name="classification">The classification of this decision</param>
        public Decision(string feature, bool classification)
        {
            Feature = feature;
            Classification = classification;
        }

        public string Feature { get; set; }
        public double PositiveCount { get; set; }
        public double NegativeCount { get; set; }
        public double LogLikelihood { get; set; }
        public bool Classification { get; set; }

        /// <summary>
        /// Increments the relevant class count for this feature.
        /// </summary>
        /// <param name="positive">The class count to be incremented</param>
        public void IncrementCount(bool positive)
        {
            if (positive)
                PositiveCount++;
            else
                NegativeCount++;
        }

        /// <summary>
        /// Adds the positive and negative counts of another decision to the counts of this decision.
        /// </summary>
        /// <param name="other">The decision whose counts should be added</param>
        public void MergeCount(Decision other)
        {
            this.PositiveCount += other.PositiveCount;
            this.NegativeCount += other.NegativeCount;
        }

        /// <summary>
        /// Returns the relevant class count for this feature
        /// </summary>
        /// <param name="positive">The class count to be returned</param>
        /// <returns>PositiveCount if positive is true, otherwise NegativeCount</returns>
        public double GetCount(bool positive)
        {
            if (positive)
                return PositiveCount;
            else
                return NegativeCount;
        }

        /// <summary>
        /// Calculates the log-likelihood of the decision and stores it. Does smoothing if either
        /// count is zero. Should only be called once counting is completed.
        /// </summary>
        public void CalculateLogLikelihood()
        {
            double total;
            if (UseComplexSmoothing)
            {
                /* The complex method does smoothing by setting the zero count to one and the non-zero count to:
                 * (count - 1)^2 + 1    [f(1) = 1, f(2) = 2, f(3) = 5, f(4) = 10, f(5) = 17...]
                 * The idea here is that if the nonzero count is very low, the log-likelihood is not very high,
                 * but if it is high then the smoothing has little effect. I still like this idea but it had
                 * a poor effect on accuracy and precision, although recall usually went up (which tells me I'd need
                 * more training and test data to get an idea of the true effect), so it would be interesting to
                 * tweak it and see if there's any effect.
                 */
                var pos = PositiveCount;
                var neg = NegativeCount;
                if (pos == 0)
                {
                    pos = 1;
                    neg = (double)Math.Pow(neg - 1, 2) + 1;
                }
                else if (neg == 0)
                {
                    pos = (double)Math.Pow(pos - 1, 2) + 1;
                    neg = 1;
                }
                total = pos + neg;

                LogLikelihood = Math.Log((pos / total) / (neg / total), 2);
                Classification = (LogLikelihood > 0);
                LogLikelihood = Math.Abs(LogLikelihood);
            }
            else
            {
                /* This smoothing strategy simply adds 1 to each count. It could be  done with only one
                 * if statement (with an or), but is written this way so that it is more clear.*/
                if (PositiveCount == 0)
                {
                    total = 1 + NegativeCount + 1;
                    LogLikelihood = Math.Log((1 / total) / ((NegativeCount + 1) / total), 2);
                }
                else if (NegativeCount == 0)
                {
                    total = PositiveCount + 1 + 1;
                    LogLikelihood = Math.Log(((PositiveCount + 1)/ total) / (1 / total), 2);
                }
                else
                {
                    total = PositiveCount + NegativeCount;
                    LogLikelihood = Math.Log((PositiveCount / total) / (NegativeCount / total), 2);
                }
            }
            
            // determine classification based on the parity of LogLikelihood
            Classification = (LogLikelihood > 0);
            // change LogLikelihood to its absolute value
            LogLikelihood = Math.Abs(LogLikelihood);
        }

        /// <summary>
        /// Compares the LogLikelihood of two Decision objects.
        /// </summary>
        /// <param name="x">The first Decision object</param>
        /// <param name="y">The second Decision object</param>
        /// <returns>A positive number if y is more likely than x, negative if it is less likely,
        /// or 0 if they are equally likely.</returns>
        public static int CompareByLikelihood(Decision x, Decision y)
        {
            return y.LogLikelihood.CompareTo(x.LogLikelihood);
        }

        /// <summary>
        /// Creates a string representation of the decision, including its feature, log-likelihood, and
        /// classification. Ultimately unused in this project, but left in for potential testing.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return Feature + "\t" + Math.Round(LogLikelihood, 4) + "\t" + (Classification ? 1 : 0);
        }
    }
}
