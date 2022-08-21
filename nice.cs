using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;
using System.Numerics;
using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.VisualBasic.FileIO;
//using StackExchange.Redis;
using System.Threading;

namespace PokerBotApplication
{
    public partial class ManualPokerBot : Form
    {
        public ManualPokerBot()
        {
            PreFlopOperations.GenerateValueMatrix(PreFlopOperations.PreflopMatrixSuited, @"suited.csv");
            PreFlopOperations.GenerateValueMatrix(PreFlopOperations.PreflopMatrixUnsuited, @"unsuited.csv");
            InitializeComponent();
        }


        private void resultBtn_Click(object sender, EventArgs e)
        {

            List<KeyValuePair<int, int>> holeCards = Parser.ToKvpList(holeCardsTxt.Text);
            List<KeyValuePair<int, int>> communityCards = Parser.ToKvpList(communityCardsTxt.Text);
            if (holeCards.Count == 2)
            {
                //try

                //{
                    if ((communityCards.Count < 3 || communityCards.Count > 5) && communityCards.Count != 0)
                    {
                        MessageBox.Show("Add your community cards!");
                    }
                    else
                    {
                        double odds = communityCards.Count == 0 ? PreFlopOperations.GetPreflopOdds(holeCards) : BotOperations.GetOdds(holeCards, communityCards);
                        int bankRoll = Regex.IsMatch(bankRollTxt.Text, @"^\d+$") && (int.Parse(bankRollTxt.Text) >= int.Parse(myChipsTxt.Text)) ? int.Parse(bankRollTxt.Text) : int.Parse(myChipsTxt.Text);
                        int betAmount = BotOperations.WhatToBet(odds, int.Parse(tableChipsTxt.Text), int.Parse(opponentBetTxt.Text), int.Parse(myChipsTxt.Text),bankRoll, out double expectedValue);
                        if (!(expectedValue > 0)) {
                            MessageBox.Show("fold!");
                        }
                        else if (betAmount == int.Parse(opponentBetTxt.Text))
                        {
                            if (int.Parse(opponentBetTxt.Text) == 0)
                            {
                                MessageBox.Show($"Your odds are {odds} and you should check for an Expected Value of +{expectedValue}$");
                            }
                            else
                            {
                                MessageBox.Show($"Your odds are {odds} and you should call for an Expected Value of +{expectedValue}$");
                            }
                          
                        }
                        else
                        {
                            if (int.Parse(opponentBetTxt.Text) == 0)
                            {
                                MessageBox.Show($"Your odds are {odds} and you should bet {betAmount} for an Expected Value of +{expectedValue}$");
                            }
                            else
                            {
                                MessageBox.Show($"Your odds are {odds} and you should raise to {betAmount} for an Expected Value of +{expectedValue}$");
                            }
                        }

                    }
                    
                  
                /*}
                catch
                {
                    MessageBox.Show("Input incorrect, did you forget to put in chip values?");
                }*/
               
            }
            else
            {
                MessageBox.Show("You need two hole cards!");
            }
        }

      
    }
    public static class Parser
    {
        private static Dictionary<char, int> _rankNumberMapping = new Dictionary<char, int> { ['T'] = 10, ['J'] = 11, ['Q'] = 12, ['K'] = 13, ['A'] = 14 };
        private static Dictionary<int, char> _numberRankMapping = new Dictionary<int, char> { [10] = 'T', [11] = 'J', [12] = 'Q', [13] = 'K', [14] = 'A' };
        private static Dictionary<int, char> _numberSuitMapping = new Dictionary<int, char> { [0] = 'c', [1] = 'd', [2] = 'h', [3] = 's' };
        private static Dictionary<char, int> _suitNumberMapping = new Dictionary<char, int> { ['c'] = 0, ['d'] = 1, ['h'] = 2, ['s'] = 3 };


        public static List<KeyValuePair<int, int>> ToKvpList(string str)
        {
            var result = new List<KeyValuePair<int, int>>();
            string pattern = @"^((^|\s)[23456789TJQKA][chds])+$";
            Match m = Regex.Match(str.TrimEnd(), pattern, RegexOptions.IgnoreCase);
            if (m.Success)
            {
                foreach (string card in str.Split(' '))
                {
                    int rank = Char.IsLetter(card[0]) ? _rankNumberMapping[card[0]] : int.Parse(card[0].ToString());
                    result.Add(new KeyValuePair<int, int>(rank, _suitNumberMapping[card[1]]));
                }
            }
            return result;
        }
        public static string ToCardString(List<KeyValuePair<int, int>> kvpList)
        {
            string str = "";
            foreach (var kvp in kvpList)
            {
                char rank = kvp.Key > 9 ? _numberRankMapping[kvp.Key] : Char.Parse(kvp.Key.ToString());
                str += rank + _numberSuitMapping[kvp.Value] + " ";
            }
            return str;
        }
    }
    public static class PreFlopOperations
    {
        public static double[,] PreflopMatrixSuited = new double[14, 14];
        public static double[,] PreflopMatrixUnsuited = new double[14, 14];

        public static void GenerateValueMatrix(double[,] matrix, string path)
        {
            using (TextFieldParser parser = new TextFieldParser(path))
            {
                bool firstLine = true;
                parser.TextFieldType = FieldType.Delimited;
                parser.SetDelimiters(",");
                parser.HasFieldsEnclosedInQuotes = true;
                int i = 0;
                int j = 0;
                while (!parser.EndOfData)
                {

                    //Processing row
                    string[] fields = parser.ReadFields();
                    if (firstLine)
                    {
                        firstLine = false;

                        continue;
                    }
                newLine:
                    bool firstField = true;
                    foreach (string field in fields)
                    {
                        if (firstField)
                        {
                            firstField = false;

                            continue;
                        }
                        if (!String.IsNullOrEmpty(field))
                        {
                            string newField = field.Replace(',', '.').Trim(new char[] { '%', '"' });
                            double percentNumber = double.Parse(newField, CultureInfo.InvariantCulture);
                            double frac = percentNumber / 100;
                            matrix[13 - i,13 - j] = frac;
                        }

                        i++;
                        if (i > 13)
                        {
                            i = 0;
                            j++;
                            goto newLine;
                        }
                    }
                }
            }
        }
        public static double GetPreflopOdds(List<KeyValuePair<int, int>> holeCards)
        {

            return holeCards[0].Value.Equals(holeCards[1].Value) ? PreflopMatrixSuited[holeCards[0].Key - 1, holeCards[1].Key - 1] : PreflopMatrixUnsuited[holeCards[0].Key - 1, holeCards[1].Key - 1];

        }
    }
    public static class BotOperations
    {

        private static double getRiskOfRuin(double odds, int i, int bankRoll)
        {
            double riskOfRuin(double units)
            {
                return Math.Pow((1 - odds) / odds, units);
            }
            return i == 0 ? 0 : riskOfRuin(bankRoll / i);


        }

        public static int WhatToBet(double odds, int tableChips, int chipOffer, int myChips, int bankRoll, out double expectedValue)
        {
            
            int bet = 0;
            double maxEv = 0;
            for (int i = chipOffer; i <= myChips + chipOffer; i++)
            {
                
                double currentEV = (odds * (tableChips + i)) + ((1 - odds) * -(i));
                if (currentEV > maxEv && (getRiskOfRuin(odds, i, bankRoll) < 0.02))
                {
                    maxEv = currentEV;
                    bet = i;
                }
            }
            expectedValue = maxEv;
            return bet;
        }

        private static (double heroPoints, double villainPoints) updateScore(ulong holes, ulong villainHoles, ulong currentHand)
        {
            (int mainScore, long tieBreaker) heroHand = HandEvaluation.HandToPlay(holes, currentHand,out ulong heroDummy);
            (int mainScore, long tieBreaker) villainHand = HandEvaluation.HandToPlay(villainHoles, currentHand,out ulong villainDummy);
            switch (heroHand.mainScore.CompareTo(villainHand.mainScore) != 0 ? heroHand.mainScore.CompareTo(villainHand.mainScore) : heroHand.tieBreaker.CompareTo(villainHand.tieBreaker))
            {
                case 1:
                    return (1, 0);
                case -1:
                    return (0, 1);
                default:
                    Debug.WriteLine($"herohand: {Convert.ToString((long)heroDummy, toBase: 2)}  {Convert.ToString((long)villainDummy, toBase: 2)}");
                    return (0.5, 0.5);
            }

        }
        public static double GetOdds(List<KeyValuePair<int, int>> holeCardsKvpList, List<KeyValuePair<int, int>> tableKvpList)
        {
            //double heroWins = 0; //if player wins
            //double villainWins = 0; //if opponent wins
            double heroWins = 0;
            double villainWins = 0;
            ulong holes = HandEvaluation.ParseAsBitField(holeCardsKvpList);
            ulong communityCards = HandEvaluation.ParseAsBitField(tableKvpList);
            ulong availableCards = HandEvaluation.Deck ^ (holes | communityCards);
            foreach (var villainHoles in HandEvaluation.CardCombos(availableCards.ToIEnum(), 2))//every combination of cards our opponent may have
            {
                
                ulong currentAvailableCards = availableCards ^ villainHoles;
                if (tableKvpList.Count < 5)
                {
                    foreach (var cardAdditions in HandEvaluation.CardCombos(currentAvailableCards.ToIEnum(), 5 - tableKvpList.Count))//all combinations of cards that may be added to the existing table
                    {
                        //Debug.WriteLine(cardAdditions);

                        ulong currentHand = communityCards | cardAdditions;
                        (double heroPoints, double villainPoints) scoreUpdate = updateScore(holes, villainHoles, currentHand);

                        heroWins += scoreUpdate.heroPoints;
                        villainWins += scoreUpdate.villainPoints;
                        //Debug.WriteLine($"hero:{heroWins} - villain:{villainWins}");
                        //heroWins += scoreUpdate.heroPoints;
                        //villainWins += scoreUpdate.villainPoints;

                    }
                }
                else
                {

                    (double heroPoints, double villainPoints) scoreUpdate = updateScore(holes, villainHoles, communityCards);

                    heroWins += scoreUpdate.heroPoints;
                    villainWins += scoreUpdate.villainPoints;

                    //Debug.WriteLine($"hero:{heroWins} - villain:{villainWins}");
                    //heroWins += scoreUpdate.heroPoints;
                    //villainWins += scoreUpdate.villainPoints;
                }

            }
            Debug.WriteLine(heroWins / (heroWins + villainWins));
            Debug.WriteLine($"hero:{heroWins} villain:{villainWins}");
            //return heroWins / (heroWins + villainWins);
            return heroWins / (heroWins + villainWins);

        }
    }
    public static class HandEvaluation
    {
        public static ulong Deck = 0b_11111111_1111100_11111111_1111100_11111111_1111100_11111111_1111100;
        public static Dictionary<ulong, (int, long)> _scoreTable = new Dictionary<ulong, (int, long)>();
        public static Dictionary<(ulong, ulong), (int, long)> _handTable = new Dictionary<(ulong, ulong), (int, long)>();
        public static Dictionary<ulong, long> _tieBreakerTable = new Dictionary<ulong, long>();
        public static Dictionary<(ulong, bool,bool), int> _mainScoreTable = new Dictionary<(ulong, bool,bool), int>();
        public static readonly object lockObj = new object();
        /*public static ConnectionMultiplexer Redis = ConnectionMultiplexer.Connect("localhost");
        public static IDatabase Db = Redis.GetDatabase();*/


        /*public static void GenerateAllKeyValuePairs()
        {
            var keys = Redis.GetServer("localhost", 6379).Keys();
            foreach (var key in keys)
            {
                if (key.ToString().Contains("fullscore"))
                {
                    string[] strArray = Db.StringGet(key).ToString().Split(" ");

                    Debug.WriteLine("hello");
                    _scoreTable[ulong.Parse(key.ToString().Remove(0,9))] = (int.Parse(strArray[0]), long.Parse(strArray[1]));
                }
                else if (key.ToString().Contains("mainscore"))
                {
                    string[] strArray = key.ToString().Remove(9).Split(" ");
                    _mainScoreTable[(ulong.Parse(strArray[0]), bool.Parse(strArray[1]))] = int.Parse(Db.StringGet(key));
                }
                else if (key.ToString().Contains("hand"))
                {
                    string[] inputArray = key.ToString().Remove(5).Split(" ");
                    string[] outputArray = Db.StringGet(key).ToString().Split(" ");
                    _handTable[(ulong.Parse(inputArray[0]), ulong.Parse(inputArray[1]))] = (int.Parse(outputArray[0]), long.Parse(outputArray[1]));
                }
            }

        }*/
        public static ulong ParseAsBitField(List<KeyValuePair<int, int>> cards)
        {
            ulong bf = 0;
            foreach (var card in cards)
            {
                bf |= 1UL << (card.Key + (15 * card.Value));
            }
            return bf;

        }
        public static string ParseAsCardString(ulong bf)
        {
            List<KeyValuePair<int, int>> kvpList = new List<KeyValuePair<int, int>>();
            for (int i = 2; i <= 14; i++)
            {
                if ((bf & (1UL << i)) > 0)
                {
                    kvpList.Add(new KeyValuePair<int, int>(i,0));
                }
                
            }
            for (int i = 17; i <= 29; i++)
            {
                if ((bf & (1UL << i)) > 0)
                {
                    kvpList.Add(new KeyValuePair<int, int>(i - 15, 0));
                }

            }
            for (int i = 32; i <= 44; i++)
            {
                if ((bf & (1UL << i)) > 0)
                {
                    kvpList.Add(new KeyValuePair<int, int>(i - 30, 0));
                }

            }
            for (int i = 47;i<=60;i++)


        }
        private static bool isStraight(int solo)
        {
            int lsb = solo & -solo;

            int normalized = solo / lsb;

            return normalized == 31 || solo == 16444;

        }
        public static int getMainScore(int solo, ulong ranksField, bool flush, bool straight)
        {
            if (_mainScoreTable.TryGetValue((ranksField, flush,straight), out int value))
            {
                return value;
            }
            if (straight && flush)
            {
                if (solo == 31744)
                {

                    return 10;
                }
                else
                {
                    return 9;
                }

            }

                /*switch (ranksField % 15)
                {
                    case 1:
                        return 8;
                    case 10:
                        return 7;

                    case 9:
                        return 4;
                    case 7:
                        return 3;
                    case 6:
                        return 2;
                    default:
                        break;
                }*/
            if ((ranksField % 15) == 1)
            {
                return 8;
            }
            if ((ranksField % 15) == 10)
            {
                return 7;
            }

            if (flush)
            {
                return 6;
            }
            if (straight)
            {
                return 5;
            }
            switch (ranksField % 15)
            {
                case 9:
                    return 4;
                case 7:
                    return 3;
                case 6:
                    return 2;
                default:
                    return 1;
            }

            
        }
        private static int getHighestRank(ulong ranksField, ref int pos)
        {
            pos = 63 - BitOperations.LeadingZeroCount((ulong)ranksField | 1);

            return (int)Math.Floor((double)(pos / 4));
        }
        private static long getTieBreaker(ulong ranksField)
        {
            if (_tieBreakerTable.TryGetValue(ranksField, out long value))
            {
                return value;
            }

            int pos = 0;
            int tiebreaker = 0;
            for (int i = 0; i < 5; i++)
            {
                int highestRank = getHighestRank(ranksField, ref pos);
                ranksField ^= (1UL << pos);
                tiebreaker |= (highestRank << (16 - (4 * i)));
            }

            //Db.StringSet("tiebreaker"+ranksField.ToString(), tiebreaker);
         
                _tieBreakerTable[ranksField] = tiebreaker;

           


            return tiebreaker;
        }
        private static void getFields(ulong bf, out int solo, out ulong ranksField, out bool flush, out bool straight)
        {
            solo = 0;
            ranksField = 0;
            flush = false;
            Dictionary<int, int> instances = new Dictionary<int, int>();
            int cards = 0;
            for (int i = 0; i < 4; i++)
            {
                int flushIdx = 0;
                for (int j = 2; j <= 14; j++)
                {


                    if ((bf & (1UL << (j + (15 * i)))) > 0)
                    {
                        cards++;
                        solo |= (1 << j);
                        flushIdx++;
                        if (flushIdx == 5)
                        {
                            flush = true;
                        }
                        if (!instances.ContainsKey(j))
                        {
                            instances.Add(j, 0);
                        }
                        else
                        {
                            instances[j] = instances[j] + 1;
                        }

                        int offset = instances[j];
                        ulong addition = 1UL << (j << 2);
                        addition = addition << offset;
                        ranksField |= addition;

                    }

                }
            }
            straight = isStraight(solo);

        }
        public static IEnumerable<ulong> ToIEnum(this ulong num)
        {
            for (int i = 2; i <= 60; i++)
            {
                if ((num & (1UL << i)) > 0)
                {
                    yield return 1UL << i;
                }
            }
        }
        public static IEnumerable<ulong> CardCombos(IEnumerable<ulong> cards, int count)
        {
            int i = 0;
            foreach (var card in cards)
            {
                if (count == 1)
                {
                    yield return card;
                }

                else
                {
                    foreach (var result in CardCombos(cards.Skip(i + 1), count - 1))
                    {

                        yield return result | card;
                    }
                }

                ++i;
            }
        }
        public static void Exchange(ref (int,long) location1, (int,long) value)
        {
            Interlocked.Exchange(ref location1.Item1, value.Item1);
            Interlocked.Exchange(ref location1.Item2, value.Item2);


        }
        public static (int mainScore, long tieBreaker) GetFullScore(ulong bf)
        {
            if (_scoreTable.TryGetValue(bf, out (int, long) value))
            {
                return value;
            }
            getFields(bf, out int solo, out ulong ranksField, out bool flush, out bool straight);
            int mainScore = getMainScore(solo, ranksField, flush, straight);
            (int mainScore, long tieBreaker) result = (mainScore, getTieBreaker(ranksField));
              _mainScoreTable[(ranksField, flush,straight)] = mainScore;
                _scoreTable[bf] = result;
            
              

            
            //Db.StringSet("fullscore"+bf, result.mainScore + " " + result.tieBreaker);
            //Db.StringSet("mainscore"+ranksField+" "+flush,mainScore);
            return result;
        }
        public static (int mainScore, long tieBreaker) HandToPlay(ulong holes, ulong cardsOnTable, out ulong maxHandBitField)
        {
            maxHandBitField = 0;
            /*if (_handTable.TryGetValue((holes, cardsOnTable), out (int, long) value))
            {
                return value;
            }*/

            (int mainScore, long tieBreaker) max = (-100000, -100000);
            foreach(var combo in CardCombos(cardsOnTable.ToIEnum(), 3))
            {
                (int mainScore, long tieBreaker) currentScore = GetFullScore(combo | holes);
                if ((currentScore.mainScore.CompareTo(max.mainScore) != 0 ? currentScore.mainScore.CompareTo(max.mainScore) : currentScore.tieBreaker.CompareTo(max.tieBreaker)) > 0)
                {

                    maxHandBitField = combo | holes;
                    max = currentScore;

                }

            }
            foreach (var combo in CardCombos(cardsOnTable.ToIEnum(), 4))
            {
                foreach (ulong holeCard in holes.ToIEnum())
                {
                    (int mainScore, long tieBreaker) currentScore = GetFullScore(combo | holeCard);
                    if ((currentScore.mainScore.CompareTo(max.mainScore) != 0 ? currentScore.mainScore.CompareTo(max.mainScore) : currentScore.tieBreaker.CompareTo(max.tieBreaker)) > 0)
                    {

                        {
                            maxHandBitField = combo | holeCard;
                            max = currentScore;

                        }

                    }
                }
            }
           
                _handTable[(holes, cardsOnTable)] = max;
            
           
            //Db.StringSet("hand"+holes + " " + cardsOnTable, max.mainScore + " " + max.tieBreaker);
            return max;
        }
    }
    
}
