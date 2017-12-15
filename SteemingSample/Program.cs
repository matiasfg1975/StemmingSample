using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.IO;
using System.Text;
using Iveonik.Stemmers;

namespace SteemingSample
{
    class Program
    {
        static void Main(string[] args)
        {
            string mText = "Take this paragraph of text and return an alphabetized list of ALL unique words.  A unique word is any form of a word often communicated with essentially the same meaning. For example, fish and fishes could be defined as a unique word by using their stem fish. For each unique word found in this entire paragraph, determine the how many times the word appears in total. Also, provide an analysis of what sentence index position or positions the word is found. The following words should not be included in your analysis or result set: \"a\", \"the\", \"and\", \"of\", \"in\", \"be\", \"also\" and \"as\".  Your final result MUST be displayed in a readable console output in the same format as the JSON sample object shown below.";

            string[] mSentences = mText.Split('.');

            StemmingSentenceProcessor stemmingProcessor = new StemmingSentenceProcessor();

            stemmingProcessor.SetExclusionList("a;the;and;of;in;be;also;as");
            foreach (string sentence in mSentences)
            {
                if (sentence != string.Empty)
                    stemmingProcessor.AddNewSentence(sentence);
            }

            Console.Write(stemmingProcessor.GetStemming());


            Console.ReadKey();
        }

    }

    [DataContract]
    public class StemmingSentenceProcessor
    {

        List<SortedSet<string>> mSteemWords = new List<SortedSet<string>>();
        IStemmer mEnglishStemmer = new EnglishStemmer();
        List<string> mExclusionList = new List<string>();


        [DataMember(Name = "result")]
        SortedList<string, StemmingWordResult> results = new SortedList<string, StemmingWordResult>();

        public void AddNewSentence(string sentence)
        {
            string[] words = CleanSentence(sentence).Split((char)32);
            SortedSet<string> values = new SortedSet<string>();

            foreach (string word in words)
            {
                values.Add(mEnglishStemmer.Stem(word));
                //Console.WriteLine(word + " --> " + englishStemmer.Stem(word));

            }
            if (values.Count > 0)
                mSteemWords.Add(values);
        }

        private string CleanSentence(string mText)
        {

            mText = mText.Replace((char)160, (char)32);
            mText = mText.Replace("\"", "");
            mText = mText.Replace(",", "");
            mText = mText.Replace(".", "");
            mText = mText.Replace(";", "");
            mText = mText.TrimStart().TrimEnd();

            return mText;
        }

        public void SetExclusionList(string exclusionList)
        {
            mExclusionList.Clear();

            string[] excludedWords = null;
            if (exclusionList != null)
                excludedWords = exclusionList.Split(';');

            foreach (string item in excludedWords)
            {
                if (!mExclusionList.Contains(item))
                    mExclusionList.Add(item);
            }

        }

        public string GetStemming()
        {
            results.Clear();
            int n = 0;
            foreach (SortedSet<string> words in mSteemWords)
            {
                foreach (string tmpValue in words)
                {
                    if (mExclusionList != null && mExclusionList.Count > 0 && !mExclusionList.Contains(tmpValue))
                    {
                        if (!results.ContainsKey(tmpValue))
                        {
                            results.Add(tmpValue, new StemmingWordResult(tmpValue, n));
                        }
                        else
                        {
                            results[tmpValue].Occurances++;
                            results[tmpValue].AddSentence(n);
                        }
                    }
                }
                n++;
            }

            return GetJsonResult();
        }

        private string GetJsonResult()
        {
            string json = string.Empty;

            if (this.results != null)
            {
                IList<StemmingWordResult> tmpValues = this.results.Values;

                try
                {
                    MemoryStream stream1 = new MemoryStream();
                    DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(IList<StemmingWordResult>));
                    ser.WriteObject(stream1, tmpValues);

                    stream1.Position = 0;
                    StreamReader sr = new StreamReader(stream1);
                    json = sr.ReadToEnd();

                }
                catch (SerializationException se )
                {
                    Console.WriteLine("Se ha producido un error serializando la información.\r\n " + se.ToString());
                }
                catch (InvalidDataContractException de )
                {
                    Console.WriteLine("Se ha producido un error serializando la información.\r\n " + de.ToString());

                }
                catch ( SystemException ee)
                {
                    Console.WriteLine("Se ha producido un error inesperador obteniendo la información.\r\n " + ee.ToString());

                }
            }
            return json;
        }

        [DataContract]
        public class StemmingWordResult
        {

            public StemmingWordResult(string word)
            {
                this.Word = word;
                this.Occurances++;
                this.Sentences = new List<int>();
            }

            public StemmingWordResult(string word, int sentence)
            {
                this.Word = word;
                this.Occurances++;
                this.Sentences = new List<int>();
                this.AddSentence(sentence);
            }

            public void AddSentence(int sentence)
            {
                if (!Sentences.Contains(sentence))
                    Sentences.Add(sentence);
            }

            [DataMember]
            public string Word { get; set; }

            [DataMember(Name = "total-occurances")]
            public int Occurances { get; set; }

            [DataMember(Name = "sentence-indexes")]
            public List<int> Sentences { get; set; }


        }
    }
}
