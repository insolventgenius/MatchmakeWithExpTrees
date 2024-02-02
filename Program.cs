using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;

class Server
{
    static void Main(string[] args)
    {

        List<Dictionary<string, string>> dataset = SetUpDataset();
        Console.WriteLine("Dataset has been setup");

        //set up the socket
        int port = 13000;
        IPAddress localAddr = IPAddress.Parse("127.0.0.1");
        TcpListener server = new TcpListener(localAddr, port);

        // Start listening for client requests.
        server.Start();

        // Buffer for reading data
        Byte[] bytes = new Byte[1024 * 4];
        String data = null;

        // Enter the listening loop.
        while (true)
        {
            Console.Write("Waiting for a connection... ");

            // Perform a blocking call to pause the while loop so I can accept requests.
            TcpClient client = server.AcceptTcpClient();
            Console.WriteLine("Connected!");

            data = null; //clear data for a new message

            // Get a stream object for reading and writing
            NetworkStream stream = client.GetStream();

            int i;

            // Loop until all of the data gets recieved
            while ((i = stream.Read(bytes, 0, bytes.Length)) != 0)
            {
                // Translate data bytes to a ASCII string.
                //we are expecting a string command
                data = Encoding.ASCII.GetString(bytes, 0, i);
                Console.WriteLine("Received: {0}", data);

                // Process the data sent by the client.
                switch(data) 

                if(data == "simulate")
                {

                // Simulate (single threaded for now) 
                dataset = RunSimulation(dataset);
                Console.WriteLine($"Simulation is completed.");

                }

                byte[] msg = Encoding.ASCII.GetBytes("Finished");

                // Send back a response.
                stream.Write(msg, 0, msg.Length);
                Console.WriteLine("Sent: {0}", data);
            }

            // Shutdown and end connection
            client.Close();
        }
    }

    static List<Dictionary<string, string>> SetUpDataset(){

        Console.WriteLine("Loading Player dataset");
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

        Console.WriteLine(lines.Length.ToString());

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
                Console.WriteLine($"{column.Key}: {column.Value}");
            }
            Console.WriteLine("-----------");
        }

        Console.WriteLine("Creating AbilityScores");
        foreach (Dictionary<string, string> row in csvData)
        {
            float kd = float.Parse(row["kdRatio"]);
            float wins = float.Parse(row["wins"]);
            float losses = float.Parse(row["losses"]);
            row["abilityScore"] = (kd * wins / losses).ToString();
            row["elo"] = "1000"; //default on first pass. 

            Console.WriteLine($"{row["name"]}'s AbilityScore is: " + row["abilityScore"]);
        }

        return csvData;
    }

    static List<Dictionary<string, string>> RunSimulation(List<Dictionary<string, string>> dataset)
    {
        List<Task> tasks = new List<Task>();
        Random random = new Random();//seed 
        //each player plays 100 players 
        for (int i= 0; i < dataset.Count; i++)
        {
            //each player plays 100 players 
            // Run a task with an anonymous lambda function
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
                float eoOpponent = 1f / 1f + (float) Math.Pow(10f, ((playerElo - opponentElo) / 400.0));
                float eoPlayer = 1f / 1f + (float)Math.Pow(10f, ((opponentElo - playerElo) / 400.0));


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
                    newPlayerRating = playerElo + 32 * (1 - playerElo);// positive if you win - score goes up 
                    newOpponentRating = opponentElo + 32 * (0 - opponentElo); //negative if you lose- score goes down 
                }
                else
                {
                    //opponent wins
                    newPlayerRating = playerElo + 32 * (0 - playerElo);// positive if you win - score goes up 
                    newOpponentRating = opponentElo + 32 * (1 - opponentElo); //negative if you lose- score goes down 
                }

                //set elo score 
                int intValue = (int)newPlayerRating;
                dataset[i]["elo"] = intValue.ToString();
                intValue = (int)newOpponentRating;
                dataset[randomInt]["elo"] = intValue.ToString();

        }
        // Wait for all the tasks to complete

        return dataset;
    }
}







