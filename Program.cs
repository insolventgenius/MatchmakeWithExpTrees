using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using static System.Formats.Asn1.AsnWriter;
using System.Reflection.Metadata.Ecma335;
using System.Collections;
using Microsoft.VisualBasic;
using Newtonsoft.Json;
using System.Runtime.InteropServices;
using static System.Net.Mime.MediaTypeNames;

class Server
{
    static void Main(string[] args)
    {
        List<Dictionary<string, string>> dataset = SetUpDataset();
        Console.WriteLine("Dataset has been setup");

        List<Dictionary<string, string>> resultsList = new List<Dictionary<string, string>>(); //for getting matches (if thats the command) 
        // Process the data sent by the client.
        //if I were to impliment this for real I would use gRPC or JSON or something like that

        while (true)
        {
            Console.WriteLine("Type \'simulate\' to simulate gameplay or type a player's name to matchmake for that player");

            string command = Console.ReadLine();
            switch (command)
            {
                case ("simulate"):
                    dataset = RunSimulation(dataset);
                    Console.WriteLine("Simulation is completed.");
                    break;
                default:
                    //assume it is a search for a player's name 
                    Console.WriteLine("Matchmaking for player: " + command);
                    resultsList = GetMatches(command, dataset);
                    if (resultsList.Count == 0)
                    {
                        Console.WriteLine("No results found");
                    }
                    else
                    {
                        Console.WriteLine($"{resultsList.Count} matches");
                        foreach (Dictionary<string, string> player in resultsList)
                        {
                            Console.WriteLine($"player name:{player["name"]}, ability score:{player["abilityScore"]}, elo:{player["elo"]} ");
                        }
                    }
                    break;
            }
        }
    }










    static List<Dictionary<string, string>> SetUpDataset() {

        //Console.WriteLine("Loading Player dataset");
        // Path to the CSV file
        string csvFilePath = @"C:\\Users\\steph\\Downloads\\CallOfDuty.csv";


        List<Dictionary<string, string>> csvData = new List<Dictionary<string, string>>();
        string[] lines = { };
        try
        {
            lines = File.ReadAllLines(csvFilePath);
        }
        catch (IOException ioe)
        {
            Console.WriteLine(ioe.Message.ToString());
        }

        //Console.WriteLine(lines.Length.ToString());

        if (lines.Length > 0)
        {
            // Assume the first line contains the headers
            var headers = lines[0].Split(',');

            // Iterate through the data lines
            for (int i = 1; i < lines.Length; i++)
            {
                var values = lines[i].Split(',');
                var row = new Dictionary<string, string>();
                for (int j = 0; j < headers.Length; j++)
                {
                    row[headers[j]] = values[j];
                }
                csvData.Add(row);
            }
        }

        // Example of how to print the data
        foreach (var row in csvData)
        {
            foreach (var column in row)
            {
                //Console.WriteLine($"{column.Key}: {column.Value}");
            }
            //Console.WriteLine("-----------");
        }

        Console.WriteLine("Creating AbilityScores");
        foreach (Dictionary<string, string> row in csvData)
        {
            float kd = float.Parse(row["kdRatio"]);
            float wins = float.Parse(row["wins"]);
            float losses = float.Parse(row["losses"]);
            row["abilityScore"] = (kd * wins / losses).ToString();
            row["elo"] = "1000"; //default on first pass. 

            //Console.WriteLine($"{row["name"]}'s AbilityScore is: " + row["abilityScore"]);
        }

        return csvData;
    }

    static List<Dictionary<string, string>> RunSimulation(List<Dictionary<string, string>> dataset)
    {
        Random random = new Random();//seed 
        //each player plays 100 players 
        for (int i = 0; i < dataset.Count; i++)
        {
            //each player plays 100 players 
            for (int x = 0; x < 100; x++)
            {
                int randomInt = random.Next(0, dataset.Count);
                //handle edge cases
                if (randomInt == i) //if you roll yourself in a match
                {
                    if (randomInt == dataset.Count - 1) //if you roll yourself in a match & you are the last in the dataset so there is no one after you 
                    {
                        randomInt--; //play the person before you in the index
                    }
                    else
                    {
                        randomInt++; //play the next person 
                    }
                }

                Dictionary<string, string> opponent = dataset[randomInt];
                Dictionary<string, string> player = dataset[i];


                float opponentElo = float.Parse(opponent["elo"]);
                float playerElo = float.Parse(player["elo"]);

                //expected outcome (score) for opponent + player
                float eoOpponent = 1f / (1f + (float)Math.Pow(10f, ((playerElo - opponentElo) / 400.0)));
                float eoPlayer = 1f / (1f + (float)Math.Pow(10f, ((opponentElo - playerElo) / 400.0)));


                //determine a winner
                float opponentAbility = float.Parse(opponent["abilityScore"]);
                float playerAbility = float.Parse(player["abilityScore"]);

                //calculate new elo score for both players
                /*So the real algorith is this 
                 * Player_A_Rating = Player_A_Old_Rating + K * (Player_A_Score - Player_A_Expected_Outcome)
                 * where K is an arbitrary multiple (in chess its 32) 
                 * For the purposes of this, we will just set the score to 1 if a player wins and 0 if a player looses
                 * 
                 */

                float newPlayerRating = 0;
                float newOpponentRating = 0;

                if (playerAbility > opponentAbility)
                {
                    //player wins
                    newPlayerRating = playerElo + (32 * (1 - eoPlayer));// positive if you win - score goes up 
                    newOpponentRating = opponentElo + (32 * (0 - eoOpponent)); //negative if you lose- score goes down 
                }
                else
                {
                    //opponent wins
                    newPlayerRating = playerElo + (32 * (0 - eoPlayer));// positive if you win - score goes up 
                    newOpponentRating = opponentElo + (32 * (1 - eoOpponent)); //negative if you lose- score goes down 
                }

                //set elo score 
                int intValue = (int)newPlayerRating;
                dataset[i]["elo"] = intValue.ToString();
                Console.WriteLine(intValue.ToString());
                intValue = (int)newOpponentRating;
                Console.WriteLine(intValue.ToString());
                dataset[randomInt]["elo"] = intValue.ToString();

            }
        }
            

        return QuickSort(dataset[0], dataset); //sort the data after simulating
    }

    //My custom implimentation of quicksort in LINQ
    static List<Dictionary<string, string>> QuickSort(Dictionary<string,string> pivotElement, List<Dictionary<string, string>> dataset)
    {
        double pivot = Convert.ToDouble(pivotElement["elo"]);

        IEnumerable<Dictionary<string, string>> moreThanPivot =
            from player in dataset
            where Convert.ToDouble(player["elo"]) > pivot
            select player;

        IEnumerable<Dictionary<string, string>> lessThanPivot =
            from player in dataset
            where Convert.ToDouble(player["elo"]) < pivot
            select player;

        List<Dictionary<string, string>> lessThanPivot_dict = lessThanPivot.ToList();
        List<Dictionary<string, string>> moreThanPivot_dict = moreThanPivot.ToList();

        //sort recursivley if you can 
        if (lessThanPivot_dict.Count > 1)
        {
            QuickSort(lessThanPivot_dict[0], lessThanPivot_dict);
        }
        if (moreThanPivot_dict.Count > 1)
        {
            QuickSort(moreThanPivot_dict[0], moreThanPivot_dict);
        }

        //merge the two datasets (which should be sorted)
        lessThanPivot.Concat(new List<Dictionary<string, string>> { pivotElement }); //since used this as a pivot, we lost this element to begin with 
        lessThanPivot.Concat(moreThanPivot); 
        //exit recursion 
        return lessThanPivot.ToList();
    }

    //get matches for the player passed in 
    //i think it makes way more sense to use the player's index in the dataset to know where it is and find matches close to it,
    //but for the purposes of LINQ/Lambda demonstration I'll use those
    static List<Dictionary<string, string>> GetMatches(string playerName, List<Dictionary<string, string>> dataset)
    {
        int index = dataset.FindIndex(a => a["name"] == playerName);//find where the player is in the sorted list

        if(index < dataset.Count - 5)
        {
            //get five players above to play with 
            List<Dictionary<string, string>> results = new List<Dictionary<string, string>>();
            results.Add(dataset[index + 1]);
            results.Add(dataset[index + 2]);
            results.Add(dataset[index + 3]);
            results.Add(dataset[index + 4]);
            results.Add(dataset[index + 5]);
            return results;
        }
        else
        {
            //get five players below the current player's index to play with 
            List<Dictionary<string, string>> results = new List<Dictionary<string, string>>();
            results.Add(dataset[index - 1]);
            results.Add(dataset[index - 2]);
            results.Add(dataset[index - 3]);
            results.Add(dataset[index - 4]);
            results.Add(dataset[index - 5]);
            return results; 
        }
    }

}







