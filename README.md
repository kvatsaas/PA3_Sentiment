# PA3_Sentiment

Below is the text of the original assignment description from CS 4242 Natural Language Processing at the Universeity of Minnesota - Duluth. The source code includes further commentary on the implementation, and the textbook referenced can be found here: https://web.stanford.edu/~jurafsky/slp3/

----

You will write three programs in that language of your choice to implement and evaluate a decision list classifier that performs sentiment analysis. Please name these programs decision-list-train, decision-list-test, and decision-list-eval, each described below. You must write three separate programs so that we can guarantee that the training and test data are never used in a program at the same time. This helps ensure a fair evalution. 

The decision list method is described in pre-recorded lecture F. Please make sure you've watched that before going on. The decision list method was originally described in Yarowsky (1994) which you may wish to read (although it is not required).  Note that even though the Yarowsky paper focuses on accent restoration, the method can be used with many classification problems, including sentiment classification. There are many variations of decision lists, but you must use the one as defined by Yarowsky. 

All of the data you need for this assignment is found in our Google Drive in directory PA-Data in a file named PA3-Pang-Lee.zip. When you unzip this you will find training data in sentiment-training.txt, test data in sentiment-test.txt and the gold standard correct classifications in sentiment-gold.txt. The training and test data are formatted such that each line consists of a single movie review. The first field in both files is the review identifier which is followed by the correct sentiment classification (in the training data only). The correct sentiment classifications for the test data are found in sentiment-gold.txt.

Your program decision-list-train must learn a decision list for sentiment classification using  unigram and bigram features and the NOT handling feature, all as described in the original Pang and Lee paper. The following example shows the process of converting a line of text into features. 

I did not like this movie at all . 

First we carry out NOT handling (prepend not_ to every word found between a negation word and an end of sentence punctuation). Note that negation words can include "not", or not as found in a contraction, as in "didn't, can't, won't, etc." Your NOT handling should deal with not and contractions of this form (n't, as in can't, won't, isn't, aren't, etc).

I did not not_like not_this not_movie not_at not_all .

Now we identify the unigrams:

I 

did 

not

not_like

not_this

not_movie

not_at

not_all

.

Then we identify the bigrams:

I did

did not

not not_like

not_like not_this

not_this not_movie

not_movie not_at

not_at not_all

not_all .

You must include unigram and bigram features and the NOT handling feature. You are free to include other features if you wish. Make sure to explain which features you are using (and why) in your program comments. You may want to experiment with different frequency cutoffs for including features - features that are very rare or very common are sometimes not very useful for classification. 

You will see examples below of how your program should be run using the Linux command line. You do not need to use Linux, but your programs should run using the same intput and output files as shown below. 

The program first program you run, decision-list-train, will have one input file considering of training data (sentiment-train.txt). This program program will learn a decision list from the training data and output that to a file (sentiment-decision-list.txt): 

decision-list-train sentiment-train.txt  > sentiment-decision-list.txt 

The decision list file (sentiment-decision-list.txt) should be a plain text file that is formatted so that each line of the file lists a feature from the decision list, the log-likelihood value associated with that feature (to 4 digits of precision), and the class that feature predicts.The decision list should be sorted in descending order of log-likelihood score. 

The second program you run, decision-list-test, will have two input files, the learned decision list (sentiment-decision-list.txt) and the test data you will evaluate your decision list on (sentiment-test.txt). This program will classify the reviews in the test data with a sentiment, and produce an output file (sentiment-system-answers.txt) that should be formatted in the same way as the gold standard data (sentiment-gold.txt).

decision-list-test  sentiment-decision-list.txt sentiment-test.txt  > sentiment-system-answers.txt

The third program you run, decision-list-eval, will have two input files, your answer file (sentiment-system-answers.txt) and the gold standard answers  (sentiment-gold.txt). This program will compare each of your system answers to the gold standard, and should output the review id, the gold answer, and the system answer (on a single line). Then, the overall accuracy, precision, and recall of your sentiment classifications should be reported at the end of the file. This should all be written to a file (sentiment-systems-answers-scored.txt)

       ./decision-list-eval sentiment-gold.txt sentiment-system-answers.txt > sentiment-system-answers-scored.txt

All three programs should be documented according to the standards of the programming assignment rubric. Remember that each program should have introductory and detailed comments as described in the programming assignment grading rubric.  

Please submit PA 3 to Canvas by 11:59 pm on the deadline. You should submit a single pdf file that includes your three programs and the output files sentiment-decision-list.txt, sentiment-system-answers.txt, and sentiment-system-answers-scored.txt. 

Please make sure your source code is displayed in black text on a white background with line numbers. 

You must write these programs "from scratch" in the language of your choice. Please do not use pre-existing libraries for NLP or Machine Learning functionality, and please do not search for example code to base your approach upon. 

For grading the functionality of your program, I will be looking at the following :

1 point - decision-list-train uses unigrams, bigrams, and NOT handling as features.

1 point - separate decision-list-train program that produces a human readable decision list sorted in decending order of log-likelihood

1 point - separate decision-list-test program that assigns sentiment to test instances

1 point - separate decision-list-eval program that produces accuracy, precision, and recall and shows test instances with gold and system assignment sentiment

1 point - accuracy reaches at least 60% (majority classifier should attain 50%).
