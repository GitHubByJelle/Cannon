# Cannon
The objective of this project was to create an advanced bot using Alpha-Beta search, incorporating necessary enhancements to surpass the performance of other students' bots in a game of [Cannon](https://www.iggamecenter.com/en/rules/cannon). As a result, the project includes a console application for Cannon and the implementation of sophisticated [alpha-beta search algorithms](https://www.chessprogramming.org/Alpha-Beta). To prepare for this project, the foundation has been laid by implementing Checkers ([https://github.com/GitHubByJelle/Checkers/](https://github.com/GitHubByJelle/Checkers/)), with its code serving as the backbone.

<p align="center" width="100%">
    <img src="images/cannon.gif" alt="Visualisation of Aspiration Search (Variable Depth = Fractional Plies, Null Move and Multi-Cut) playing a game of Cannon against itself" width="70%">
</p>

## Implementation Details
The code is written in C#. No packages have been used for the implementations of the search algorithms. For AI functionality, the code leverages the following techniques/algorithms:
* [Genetic Algorithm](https://www.chessprogramming.org/Genetic_Programming)
* One-ply search
* [Alpha-Beta Search](https://www.chessprogramming.org/Alpha-Beta)
* [Transposition Tables](https://www.chessprogramming.org/Transposition_Table)
* [Zobrist Hashing](https://www.chessprogramming.org/Zobrist_Hashing)
* [Forsyth-Edwards Notation](https://www.chessprogramming.org/Forsyth-Edwards_Notation) (FEN)

A good linear feature evaluation function is implemented. Since adding to many features leads to the Horizon Effect, features are selected carefully. The weights of the evaluation function are tuned by hand and the Genetic Algorithm, resulting in an evaluation function that really likes having cannons. The following features have been used:
* [Material](https://www.chessprogramming.org/Material) (Pieces) (Current - Enemy) (MAX)
* Material (Soldier) (Current - Enemy) (MAX)
* Material (Cannon) (Current - Enemy) (MAX)
* Material (Town) (Current - Enemy) (MAX)
* [Control](https://www.chessprogramming.org/Square_Control) (Current - Enemy) (MAX)
* [Danger](https://www.chessprogramming.org/Threat_Move) (Pieces) (Current - Enemy) (MIN)
* Danger (Soldier) (Current - Enemy) (MIN)
* Danger (Cannon) (Current - Enemy) (MIN)
* Danger (Town) (Current - Enemy) (MIN)
* [Mobility](https://www.chessprogramming.org/Mobility) (Possible moves) (Current - Enemy) (MAX)
* [Random factor](https://www.chessprogramming.org/Search_with_Random_Leaf_Values)

The following enhancements are added to the alpha-beta search bot:
* [Iterative Deepening](https://www.chessprogramming.org/Iterative_Deepening) (stop when time runs out)
* Move ordering based on previous search, using:
    * Transposition Tables (64bit Zobrist Hashing, using the first 20bits. Type II error is replaced based on depth)
    * [Killer Move](https://www.chessprogramming.org/Killer_Move) (Store newest, one per level)
    * Knowledge  (Shoot > Capture > Retreat > Slide > Step)
    * ([Relative](https://www.chessprogramming.org/Relative_History_Heuristic)) [History Heuristic](https://www.chessprogramming.org/History_Heuristic) (Increments with 1, devides by 16 when search is done)
* Search window
    * [Aspiration Windows](https://www.chessprogramming.org/Aspiration_Windows) (Performed better than NegaScout)
    * [NegaScout](https://www.chessprogramming.org/NegaScout#:~:text=NegaScout%20just%20searches%20the%20first,zero%20window%20at%20PV%2DNodes.)
* Variable depth (Allows the bot to search 3 plies deeper)
    * Forward pruning
        * [Null Move](https://www.chessprogramming.org/Null_Move_Pruning) (Not in root move or consecutive, R = 2)
        * [Multi-Cut](https://www.chessprogramming.org/Multi-Cut) (R=2, C=3, M=10)
        * Not used in endgame (when one of the player has less than 8 pieces)
    * Extensions
        * [Fractional Plies](https://www.chessprogramming.org/Depth#FractionalPlies) (If shoot or capture, reduce depth with 0.5, else 1)

# Making the bot Tournament ready
As mentioned, one of the course requirements is to play a tournament against other players. Because of this a human player is implemented as well. The user can perform legal moves, using the same notation as [iggamecenter](https://www.iggamecenter.com/en/rules/cannon), but can also undo a move.

To prevent a loss when my client crashes, a FEN string has been used to store the most recent board position before the crash.

During the tournament, both players get 10 minutes in total to finish the game. If the time runs out, the player loses. To prevent my bot from using too much time per move, Monte Carlo Evaluation has been used to determine the available time for the next move. It calculated the avaialble time by playing 10 games using the Heuritic bot ($\epsilon$-greedy) when waiting for the opponents turn.

## Implemented bots
Based on the observed search times, search depth and performances, the most "optimal" bot is implemented. This bot contains:
* Move Ordering: Knowledge and Transposition Tables
* Search Windows: No technique
* Variable Depth: Fractional Plies and Null Move

However, to implement this bot, a lot of other bots have been implemented:
1. Human.
2. RandomBot (Easy).
3. HeuristicBot (Epsilon greedy)
4. Iterative Bot (no move ordering)
5. Iterative Bot (Move ordering on knowledge).
6. Iterative Bot with TT.
7. Iterative Bot with TT and KM.
8. Ordered Iterative Bot (TT, KM and HH).
9. Principal Variation Search / NegaScout.
10. Aspiration Search.
11. Aspiration Search with Fractional Plies and Null Moves.
12. Aspiration Search (Variable Depth = Fractional Plies, Null Move and Multi-Cut).
13. Aspiration Search using Monte Carlo Evaluation to adjust time (Variable Depth).
14. Aspiration Search (Variable Depth, replaced HH with Relative HH)
15. 'Optimal' bot (only 'optimal' improvements added)

## How to use
To run the game, open Visual Studio (2019), open the folder and run `Programm.cs`. 

To create a new .exe file, run the code and see the "bin" folder (e.g. the "Release" folder).

## Known improvements
When implementing several improvements have been noticed. These are:
* Improve evaluation function
    * Fine-tune the weights more optimally (e.g. with TD-Learning)
    * Improve features
* Optimize code
    * Create less objects
    * Parallize code
    * Sorting moves efficiently and calculating features
* Create GUI
    * As the assignment for which this project was undertaken does not necessitate visualizations, no visual representations have been incorporated, except for those displayed in the terminal. It would be nice to add a GUI.
* The code still contains code some experiments / comments for optimising the code (e.g. multiple sorting / evaluation functions). I kept them in, to give in-depth detail on the things I tried.
