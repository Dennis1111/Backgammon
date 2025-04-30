Backgammon Project
This repository contains a multi-project solution for Backgammon, including a console application for training and learning, a test project for validation, and a web application for playing against a computer opponent.
Project Overview

1. Backgammon Console App
The console application is designed to train and simulate Backgammon games. It uses advanced algorithms to learn and improve gameplay strategies over time.
Key features:
•	Simulates Backgammon games for training purposes.
•	Implements AI learning to improve decision-making.
•	Outputs game data for analysis and debugging.
•	With some interval it stores a game to a file that can be loaded with the currently strongest backgammon software XG (Extreme Gammon) to see how well it plays.
AI
  The backgammon-bot uses reinforcement learning with neural networks to learn the game.
  I have been implementing a variant of NNUE that is used in the strongest chess engines as Stockfish. 
  The idea is that in chess few inputs changes after each move so we don't need to recalculate the whole network,
  in backgammon we have more inputs though that changes for each but you can still make some gain.
  The neural networks runs on the cpu with SIMD instructions to evaluate the neural net as fast as possible.
  They AI tries to find the best move where it also consider loosing gammons and backgammons (but the cube is not implemented yet so it basically plays moneygame without the cube and Jacoby rule)
To start the console app change dir to Backgammon, then start it with dotnet run.
This app uses a data folder where it stores neural networks and a bearoff database. If the bearoff database is missing it will take quite some time to build it the first time.


2. Backgammon.Tests
The test project ensures the correctness and stability of the Backgammon logic. It includes unit tests for some of the most complex functions and neural networks.
Key features:
•	Validates core Backgammon logic.
•	Ensures that changes to the codebase do not introduce regressions.
•	Uses NUnit and Playwright for testing.

3. Backgammon.WebApp
The web application allows users to play Backgammon against a computer opponent. It provides an interactive UI built with Blazor and a backend powered by .NET 9.
I have just started on this app and only works partially yet.
Key features:
•	Play Backgammon in a browser vs AI.
•	AI-powered computer opponent.
•	API endpoints for move validation and computer moves.
---
Getting Started
Prerequisites
•	.NET 9 SDK
•	A modern web browser
InstallationAn AI backgammon project using reinforcement learning.
