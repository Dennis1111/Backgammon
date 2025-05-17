# Backgammon Project

This repository contains a multi-project solution for Backgammon, including:

- A console application for training and learning
- A test project for validation
- A web application for playing against a computer opponent

---

## Project Overview

### 1. Backgammon Console App

The console application is designed to train and simulate Backgammon games. It uses advanced algorithms to learn and improve gameplay strategies over time.

**Key features:**

- Simulates Backgammon games for training purposes
- Implements AI learning to improve decision-making
- Outputs game data for analysis and debugging
- Periodically stores a game file that can be loaded into XG (Extreme Gammon) to evaluate its performance

How to run:
cd Backgammon
dotnet run

**AI**

The backgammon-bot uses reinforcement learning with neural networks to learn the game.  
A variant of **NNUE** (used in top chess engines like Stockfish) is being implemented.  
In chess, few inputs change per move, so the network doesn't need full recomputation.  
Backgammon has more input changes, but partial optimization is still possible.

- Neural networks run on the CPU with SIMD instructions for fast evaluation
- The AI evaluates moves based on potential loss of gammons and backgammons
- The doubling cube is not yet implemented — it plays as a money game without the cube and Jacoby rule

### 2. Backgammon tests
The test project ensures the correctness and stability of the Backgammon logic.
It includes unit tests for some of the most complex functions and neural networks.

### 3. Backgammon.WebApp
The web application allows users to play Backgammon against a computer opponent.
It features an interactive UI built with Blazor and a backend powered by .NET 9.
To play vs AI:
1. cd Backgammon.WebApp
2. dotnet run
3. visit http://localhost:5233/play-game in browser