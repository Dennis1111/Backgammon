using Microsoft.AspNetCore.Mvc;
using Backgammon.GamePlay;
using Backgammon.Models;
using Backgammon.WebApp.Dtos;
using static Backgammon.Models.BackgammonBoard;

[ApiController]
[Route("api/[controller]")]
public class GameController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new { message = "Hello from API!" });
    }

    [HttpGet("generate-game")]
    public IActionResult GenerateGame()
    {
        GameSimulator gameSimulator = new();
        GameData gameData = gameSimulator.PlayMoneyGame();

        var response = new List<MoveDto>();
        string? finalScore = null;

        for (int i = 0; i < gameData.MoveData.Count; i++)
        {
            var move = gameData.MoveData[i];

            if (i < gameData.MoveData.Count - 1)
            {
                // Regular move data
                response.Add(new MoveDto
                {
                    Board = move.BoardBefore,
                    MoveAnnotation = move.Move.MovesAsStandardNotation(), // Directly assign without string.Join
                    Die1 = move.Move.Die1,
                    Die2 = move.Move.Die2,
                    Player = move.Player,
                    ScoreEstimation = move.ScoreVector?.Select(score => Math.Round(score, 2)).ToList()
                });
            }
            else
            {
                // Final move data with score or result
                finalScore = "Player 1 Wins";  // Update based on your game logic to calculate winner

                response.Add(new MoveDto
                {
                    Board = move.BoardAfter,
                    MoveAnnotation = "Final Position", // Or provide a specific notation for the final move
                    Die1 = move.Move.Die1,
                    Die2 = move.Move.Die2,
                    Player = move.Player,
                    ScoreEstimation = move.ScoreVector?.Select(score => Math.Round(score, 2)).ToList(),
                    FinalScore = finalScore
                });
            }
        }
        return Ok(response); // Return the list of moves with final score information
    }

    [HttpPost("computer-move")]
    public IActionResult GenerateComputerMove([FromBody] GameStateDto gameState)
    {
        var game = new GameSimulator();
        MoveData? computerMove = game.GenerateComputerMove(gameState.Board, gameState.Die1, gameState.Die2, gameState.Player);
        ComputerMoveResponseDto response;
        if (computerMove is null)
        {
            response = new ComputerMoveResponseDto
            {
                Move = null,
                BoardAfterMove = gameState.Board,
            };
        }else
            response = new ComputerMoveResponseDto
            {
                Move = computerMove.Move,
                BoardAfterMove = computerMove.BoardAfter
            };
        Console.WriteLine($"Board after: {response.BoardAfterMove}");
        return Ok(response);
    }

    [HttpPost("valid-moves")]
    public IActionResult GenerateLegalMoves([FromBody] GameStateDto gameState)
    {
        var legalMovesRaw = GenerateLegalMovesStatic(gameState.Board, gameState.Die1, gameState.Die2, gameState.Player);
        var response = new LegalMovesResponseDto
        {
            LegalMoves = [.. legalMovesRaw.Select(m => new MoveResult
            {
                Move = m.move,
                Board = m.board
            })]
        };

        return Ok(response);
    }
}