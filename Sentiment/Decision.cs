using System;

namespace Sentiment
{
    public class Decision
    {
        static public readonly bool UseComplexSmoothing = false;

        public Decision(string feature)
        {
            Feature = feature;
            PositiveCount = 0.0;
            NegativeCount = 0.0;
        }

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

        public void IncrementCount(bool positive)
        {
            if (positive)
                PositiveCount++;
            else
                NegativeCount++;
        }

        public void MergeCount(Decision other)
        {
            this.PositiveCount += other.PositiveCount;
            this.NegativeCount += other.NegativeCount;
        }

        public double GetCount(bool positive)
        {
            if (positive)
                return PositiveCount;
            else
                return NegativeCount;
        }

        public void CalculateLogLikelihood()
        {
            double total;
            if (UseComplexSmoothing)
            {
                if (PositiveCount == 0)
                {
                    PositiveCount = (double)Math.Pow(PositiveCount + 1, 2);
                    NegativeCount = (double)Math.Pow(NegativeCount - 1, 2) + 1;
                }
                else if (NegativeCount == 0)
                {
                    PositiveCount = (double)Math.Pow(PositiveCount - 1, 2) + 1;
                    NegativeCount = (double)Math.Pow(NegativeCount + 1, 2);
                }
                total = PositiveCount + NegativeCount;

                LogLikelihood = Math.Log((PositiveCount / total) / (NegativeCount / total), 2);
                Classification = (LogLikelihood > 0);
                LogLikelihood = Math.Abs(LogLikelihood);
            }
            else
            {
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
            

            Classification = (LogLikelihood > 0);
            LogLikelihood = Math.Abs(LogLikelihood);
        }

        public static int CompareByLikelihood(Decision x, Decision y)
        {
            return y.LogLikelihood.CompareTo(x.LogLikelihood);
        }

        public override string ToString()
        {
            return Feature + "\t" + Math.Round(LogLikelihood, 4) + "\t" + (Classification ? 1 : 0);
        }
    }
}
