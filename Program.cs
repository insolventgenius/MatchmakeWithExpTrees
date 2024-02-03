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
using System.Linq.Expressions;
using System.Reflection;

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
            Console.WriteLine("\nType \'simulate\' to simulate gameplay, \ntype \'list players\' for the entire dataset sorted by elo, \nor type a player's name to matchmake for that player\n");
            Console.Write("input:");
            string command = Console.ReadLine();
            switch (command)
            {
                case ("simulate"):
                    dataset = RunSimulation(dataset);
                    Console.WriteLine("Simulation is completed.");
                    break;
                case ("list players"):
                    Console.WriteLine("=====================================================================");
                    foreach(Dictionary<string, string> player in dataset)
                    {
                        Console.WriteLine($"player name:{player["name"]}, ability score:{player["abilityScore"]}, elo:{player["elo"]} ");
                    }
                    Console.WriteLine("=====================================================================");
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

        Console.WriteLine("Loading Player dataset");
        string exeDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        Directory.SetCurrentDirectory(exeDir);
        // Path to the CSV file
        string csvFilePath = @"CallOfDuty.csv";


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

                //non-Exp Tree pattern
                //expected outcome (score) for opponent & player
                //float eoOpponent = 1f / (1f + (float)Math.Pow(10f, ((playerElo - opponentElo) / 400.0)));
                //float eoPlayer = 1f / (1f + (float)Math.Pow(10f, ((opponentElo - playerElo) / 400.0)));


                //===================================================================================
                //expected outcome (score) for opponent & player
                //exp tree pattern 
                var eoPlayerExpression = EloExpressionBuilder.BuildEoExpression(true); //player perspective
                var eoOpponentExpression = EloExpressionBuilder.BuildEoExpression(false); //opponent perspective

                // Compile the expressions to get the functions
                Func<float, float, float> eoPlayerFunc = eoPlayerExpression.Compile();
                Func<float, float, float> eoOpponentFunc = eoOpponentExpression.Compile();

                // Now you can use eoPlayerFunc and eoOpponentFunc to calculate Eo values
                float eoPlayer = eoPlayerFunc(playerElo, opponentElo);
                float eoOpponent = eoOpponentFunc(playerElo, opponentElo);

                Console.WriteLine($"EO Player: {eoPlayer}, EO Opponent: {eoOpponent}");


                //===================================================================================

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
                //Console.WriteLine(intValue.ToString());
                intValue = (int)newOpponentRating;
                //Console.WriteLine(intValue.ToString());
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
            lessThanPivot_dict = QuickSort(lessThanPivot_dict[0], lessThanPivot_dict);
        }
        if (moreThanPivot_dict.Count > 1)
        {
            moreThanPivot_dict = QuickSort(moreThanPivot_dict[0], moreThanPivot_dict);
        }

        //merge the two datasets (which should be sorted)
        lessThanPivot_dict.Concat(new List<Dictionary<string, string>> { pivotElement }); //since used this as a pivot, we lost this element to begin with 
        lessThanPivot_dict.Concat(moreThanPivot_dict);
        //exit recursion 
        // Merge using LINQ
        return lessThanPivot_dict
            .Concat(new List<Dictionary<string, string>> { pivotElement })
            .Concat(moreThanPivot_dict)
            .ToList();
    }

    //get matches for the player passed in 
    //i think it makes way more sense to use the player's index in the dataset to know where it is and find matches close to it,
    //but for the purposes of LINQ/Lambda demonstration I'll use those
    static List<Dictionary<string, string>> GetMatches(string playerName, List<Dictionary<string, string>> dataset)
    {
        playerName = playerName.ToLower();
        int index = dataset.FindIndex(a => a["name"].ToLower() == playerName);//find where the player is in the sorted list

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


public class EloExpressionBuilder
{
    public static Expression<Func<float, float, float>> BuildEoExpression(bool isPlayer)
    {
        ParameterExpression playerElo = Expression.Parameter(typeof(float), "playerElo");
        ParameterExpression opponentElo = Expression.Parameter(typeof(float), "opponentElo");

        // because we have to flip the player's elos in the formula based on perspective 
        //eg. if I have a high expecatation, they have a low expectation
        Expression eloDifference = isPlayer ?
            Expression.Subtract(opponentElo, playerElo) :
            Expression.Subtract(playerElo, opponentElo);

        ConstantExpression divisor = Expression.Constant(400.0f, typeof(float));
        Expression divisionResult = Expression.Divide(eloDifference, divisor);

        MethodCallExpression exponentResult = Expression.Call(typeof(Math).GetMethod("Pow", 
            new Type[] { typeof(double), typeof(double) }),
            Expression.Constant(10.0),
            Expression.Convert(divisionResult, typeof(double))
            
        );

        // Calculate 1 / (1 + 10^(difference / 400)).
        ConstantExpression one = Expression.Constant(1.0f, typeof(float));
        BinaryExpression denominator = Expression.Add(
            one,
            Expression.Convert(exponentResult, typeof(float)) // Convert double result back to float
        );
        BinaryExpression eoExpression = Expression.Divide(one, denominator);

        return Expression.Lambda<Func<float, float, float>>(eoExpression, playerElo, opponentElo);
    }
}







