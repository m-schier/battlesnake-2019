# BattleSnake 2019 Submission by Team "Niedersächsische Kreuzotter"

## About
BattleSnake 2019 was an artificial intelligence competition carried out by Dyspatch, Github and other sponsors in March 2019 in Victoria, BC, Canada.
The objective of the competition was creating an AI to control a snake in a multi agent game of competetive snake dubbed BattleSnake.

This repository contains the submission of our team consisting of Maximilian Schier, Frederick Schubert and Niclas Wüstenbecker (in alphabetical order).
The team efforts were supported by the Institute for Information Processing (TNT) of the University of Hannover, especially Florian Kluger, M.Sc.

Our team placed 2nd in the Intermediate division.

## Content
The code in this repository is a heavily cut and edited version of our tournament code. Many agents, heuristics, metrics and functions that are not required
to run were stripped. This controller does however perform exactly the same moves as it did during competition.

## Installation and running
This project requires [.NET Core SDK](https://dotnet.microsoft.com/download) for compiling. To build and run this solution, run `dotnet run -c Release` from this
folder. A web server is started on the default port of 5050, though this port may be changed through the `PORT` environment variable. You may then interface with the
controller on `http://localhost:5050/primary` with the [official BattleSnake engine](https://github.com/battlesnakeio/engine).

## Strategy
Our AI performs a game tree search to determine the optimal move. Therefore we had to implement a full simulation of the game in C# to simualted future steps.
We implemented a different world representation than the official engine uses that has very fast constant time update, cloning and collision checking by using
a matrix/pointer based representation of the world instead of the list based approach of the main engine. This potential was however not fully used as our heuristics
proved to be the main bottleneck in the end.

Game tree search is iteratively deepened until the specified cutoff time is reached. The subset of opponents to simulate is determined by minimum distance of an opponents
body part to our own head. If we cannot reach any body part within the search depth, we are performing a reflex based evaluation for this opponent. This means that the opponent
will try to perform a non-suicidal move at each leaf of the search tree, but we are not playing out all moves for this opponent. If the number of fully simulated snakes (including
our own) is 2, we are performing Alpha Beta search for its increased speed (though this search could still be improved by sorting of nodes and other means usually employed).
If we are however simulating more than 2 snakes, we are employing the MaxN algorithm for multi agent games. This algorithm uses no pruning and can therefore only evaluate a
significantly more shallow part of the tree.

Due to the nature of both Alpha Beta and MaxN search our agent is generally pessimistic about its options. Since we are performing the first virtual ply followed by the opponents
ply during search, the opponent may counter our move perfectly for all cases. This is however not possible in reality as our ply is not visible until all agents have specified
their desired moves.

For heuristic we primarily used a variety of flood fill and distance metrics. 

## Remarks
The deepening search using MaxN simulating all potentially dangerous opponents allowed us to play very well in the early stages of the game, rarely getting eliminated.
This also resulted in us performing best of all submitting teams in the Dyspatch arena where the agent faced an increasing number of hostile snakes ganging up on the
user snake, winning this bounty.

It became apperant however that our heuristics were sub par. Our agent tries to maximize its territorial advantage without utilizing either length or territory advantage
fully. This generally resulted in us "idling" too much, which is also the reason we had a significant length disadvantage in the final duell against the winner team.

Should we compete again, we should mainly focus on improving our heuristics for this reason.

## Known issues
- Receiving an `/end` request with our snake killed results in a non fatal exception causing a resource leak