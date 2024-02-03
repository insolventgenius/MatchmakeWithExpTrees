<!-- markdownlint-disable-next-line --><div align="center">
 ## Stephen's Matchmaking Server
 
I have created a program inspired by a matchmaking server. This is an implimentation of [elo ranking](https://en.wikipedia.org/wiki/Elo_rating_system) using LINQ and
[Expression Trees](https://learn.microsoft.com/en-us/dotnet/csharp/advanced-topics/expression-trees/) in C#. 
</div>
	

## Instructions
There are 3 (case sensative) commands: "simulate", "list players", and *player name* [ex. "Player 3"]
The "CallOfDuty.csv" dataset needs to be in the same directory as the executable! 
							
 	simulate - all players in the dataset "play" eachother
	list players - prints all of the dataset to the screen
	*player name* - retrieves 5 opponents via skill based matchmaking


## Background
I took a Call of Duty dataset from Kaggle. I removed all of the usernames & replaced them
with generic "Player n" aliases so they are easy to search via cli. Using the simulate command, the 
players will all "play" eachother. Everyone is assigned a base elo score of 1000. I use LINQ to lookup 
the players from the data set when one is queried. I use an Expression Class to dynamically build an expected outcome
for each player based on the perspective. So if I am a low-skill player, my odds are low but if I am high-skilled, 
my expected outcome is high. 

Ability Score
I use their actual statics in game to come up with an "Ability Score" 
which gives me a deterministic way of achieving a set of results for the algorithm. (So you can see that it works) 
So basically, the ability score is how the real player actually plays and it is the server's job to determine their skill 
level based on how they play and then rank them accordingly.

Matches
Each player plays 100 players per simulation. An expected outcome is determined based on the delta bewteen each player's elo before each game. 
Whoever has the higher Ability Score 'wins' and then the elo ranks for each player are adjusted accordingly.

## Conclusion
You should see that as more games are played, players with a similar ability are sorted closer together over time. 
