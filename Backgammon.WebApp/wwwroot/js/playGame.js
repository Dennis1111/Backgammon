import { updateBoard } from './updateBoard.js';
import {
    Player1, Player2, AcePointP1, AcePointP2, SixPointP1, SixPointP2,
    EightPointP1, EightPointP2, MidPointP1, MidPointP2, OnTheBarP1,
} from './boardConstants.js';

function startingPosition() {
    // Initialize the board with 28 points for the 24 standard points,
    // plus positions for the bar and bear-off
    const Position = new Array(28).fill(0);

    // The bottom (Player 1) side
    Position[AcePointP1] = -2;
    Position[SixPointP1] = 5;
    Position[EightPointP1] = 3;
    Position[MidPointP2] = -5;

    // The top (Player 2) side
    Position[AcePointP2] = 2;
    Position[SixPointP2] = -5; // Player 2 starting position
    Position[EightPointP2] = -3;
    Position[MidPointP1] = 5;

    return Position;
}

export async function initializeGame() {
    let scoreP1 = 0;
    while (true) {
        let score = await playGame();
        scoreP1 += score;
    }

    attachPointListeners();

    window.addEventListener('resize', debounce(() => {
        updateBoard(board, die1, die2, player);
    }, 150));
}


class GameState {
    constructor() {
        this.isWaitingForHumanMove = false;
        this.selectedMoves = [];
        this.validMovesForCurrentTurn = [];
        this.resolveHumanMove = null;
    }

    startHumanMove(validMoves) {
        this.isWaitingForHumanMove = true;
        this.selectedMoves = [];
        this.validMovesForCurrentTurn = validMoves;
        return new Promise((resolve) => {
            this.resolveHumanMove = resolve;
        });
    }

    completeHumanMove() {
        this.isWaitingForHumanMove = false;
        if (this.resolveHumanMove) {
            this.resolveHumanMove(this.selectedMoves);
        }
    }
}

const gameState = new GameState();

function buildBoardFromDOM() {
    const board = new Array(28).fill(0); // Initialize the board with 28 points

    // Iterate over all points on the board
    document.querySelectorAll('.point').forEach(point => {
        const pointId = parseInt(point.id.replace('point-', ''), 10); // Extract the point index from the ID
        const checkers = point.querySelectorAll('.checker');

        if (checkers.length > 0) {
            // Determine ownership based on the first checker (all checkers on a point belong to the same player)
            const isPlayer1 = checkers[0].classList.contains('player1');
            const checkerCount = checkers.length;

            // Positive for Player 1, negative for Player 2
            board[pointId] = isPlayer1 ? checkerCount : -checkerCount;
        }
    });
    return board;
}

function handlePointClick(pointElement) {
    if (!gameState.isWaitingForHumanMove) return;
    const board = buildBoardFromDOM(); // Build the board state from the DOM
    const pointId = parseInt(pointElement.dataset.pointId, 10);
    const die1 = parseInt(document.getElementById('dice1-player1').dataset.value, 10);
    const die2 = parseInt(document.getElementById('dice2-player1').dataset.value, 10);

    const selectedDice = gameState.selectedMoves.length === 0 ? die1 : die2;

    // Check if there are checkers on the bar
    if (board[OnTheBarP1] > 0) {
        if (pointId === OnTheBarP1) {
            let from = OnTheBarP1;
            let to;

            if (isNotBlockedForP1(OnTheBarP1 - die1)) {
                to = OnTheBarP1 - die1;
            } else if (isNotBlockedForP1(OnTheBarP1 - die2)) {
                to = OnTheBarP1 - die2;
            } else {
                console.log("No valid moves from the bar.");
                return;
            }

            gameState.selectedMoves.push(createCheckerMove(from, to));

            if (hittingP2(to)) {
                board[from]--;
                board[OnTheBarP2]++;
                board[to] = 1;
            } else {
                board[from]--;
                board[to]++;
            }

            updateBoard(board);
        } else {
            console.log("You must move checkers from the bar first.");
        }
    } else {
        const from = pointId;
        const to = from - selectedDice;

        if (isNotBlockedForP1(to)) {
            gameState.selectedMoves.push(createCheckerMove(from, to));

            if (hittingP2(to)) {
                board[from]--;
                board[OnTheBarP2]++;
                board[to] = 1;
            } else {
                board[from]--;
                board[to]++;
            }

            updateBoard(board);
        } else {
            console.log("Move is blocked.");
        }
    }

    if (gameState.selectedMoves.length === gameState.validMovesForCurrentTurn.length) {
        gameState.completeHumanMove();
    }
}

function rollDice(diceMustDiffer = false) {
    const die1 = Math.floor(Math.random() * 6) + 1; // Roll the first die (1-6)
    let die2 = Math.floor(Math.random() * 6) + 1; // Roll the second die (1-6)

    // If diceMustDiffer is true, ensure die2 is different from die1
    if (diceMustDiffer) {
        while (die2 === die1) {
            die2 = Math.floor(Math.random() * 6) + 1;
        }
    }
    return [die1, die2];
}

// This function is now async, since it involves an asynchronous fetch
async function fetchComputerMove(board, die1, die2, player) {
    const payload = {
        board,
        die1,
        die2,
        player
    };

    const response = await fetch('api/game/computer-move', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(payload)
    });

    if (!response.ok) {
        throw new Error('Failed to get computer move');
    }

    return await response.json(); // Returning the data from the API
}

async function fetchValidMoves(board, die1, die2, player) {
    const payload = {
        board,
        die1,
        die2,
        player
    };

    const response = await fetch('api/game/valid-moves', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(payload)
    });

    if (!response.ok) {
        throw new Error('Failed to get valid moves');
    }

    return await response.json(); // Returning the valid moves from the API
}

async function getHumanMove(validMoves) {
    return gameState.startHumanMove(validMoves);
}




// Function to swap between player 1 and player 2
function swapPlayer(player) {
    return player === Player1 ? Player2 : Player1;
}

// This function calling fetchComputerMove is also async
async function playGame() {
    let score = 0;
    let [die1, die2] = rollDice(true);
    let player = die1 > die2 ? Player1 : Player2;
    let board = startingPosition();
    //updateBoard(board, die1, die2, player);
    let moveCount = 0;
    let gameEnded = false;
    let gameMoves = [];

    while (!gameEnded) {
        if (moveCount > 0) {
            [die1, die2] = rollDice(false);
        }
        updateBoard(board, die1, die2, player);
        if (player === Player2) {
            // Awaiting the computer move
            const { move, boardAfterMove } = await fetchComputerMove(board, die1, die2, player);
            board = boardAfterMove;
            gameMoves.push(move);
        } else {
            console.log("Human Move1"+board);
            // Fetch valid moves for the current player
            const { legalMoves } = await fetchValidMoves(board, die1, die2, player);
            if (legalMoves.length > 0) {
                // Get a random human move from valid moves
                const { move, boardAfterMove } = await getHumanMove(legalMoves,board);

                board = boardAfterMove;
                console.log("Human Move2" + board);
                gameMoves.push(move);
            }
            console.log("Human can't move" + board);
        }

        moveCount++;
        player = swapPlayer(player);
        await new Promise(resolve => setTimeout(resolve, 1500));

        //gameEnded = checkIfGameEnded(board); // Assuming this checks if the game is over
    }

    return score;
}

/*async function getHumanMove(validMoves, board) {
const selectedMoves = [];
//debugger;
//let currentDice = document.getElementById('dice1-player1').textContent; // Assuming dice values are stored in elements
//let selectedDice;

return new Promise((resolve) => {
function handlePointClick(event) {
const pointId = parseInt(event.target.dataset.pointId, 10); // Get the clicked point ID
const die1 = parseInt(document.getElementById('dice1-player1').dataset.value, 10);
const die2 = parseInt(document.getElementById('dice2-player1').dataset.value, 10);
debugger;
// Determine which dice to use , needs improvement
if (selectedMoves.length === 0) {
selectedDice = die1;
} else {
selectedDice = die2;
}

// Check if there are checkers on the bar
if (board[OnTheBarP1] > 0) {
 
if (pointId === OnTheBarP1) {
    let from = OnTheBarP1;
    let to;

    // Determine the destination point based on dice
    if (isNotBlockedForP1(OnTheBarP1 - die1)) {
        to = OnTheBarP1 - die1;
    } else if (isNotBlockedForP1(OnTheBarP1 - die2)) {
        to = OnTheBarP1 - die2;
    } else {// This should not happen if validMoves is not empty and then we should be in this function
        console.log("No valid moves from the bar.");
        return;
    }

    selectedMoves.push(createCheckerMove(from, to));

    // Handle hitting opponent's checkers
    if (hittingP2(to)) {
        board[from]--;
        board[OnTheBarP2]++;
        board[to] = 1;
    } else {
        board[from]--;
        board[to]++;
    }

    updateBoard(board); // Update the graphics
} else {
    console.log("You must move checkers from the bar first.");
}
} else {
// Handle moves from other points
const from = pointId;
const to = from - selectedDice;

if (isNotBlockedForP1(to)) {
    selectedMoves.push(createCheckerMove(from, to));

    // Handle hitting opponent's checkers
    if (hittingP2(to)) {
        board[from]--;
        board[OnTheBarP2]++;
        board[to] = 1;
    } else {
        board[from]--;
        board[to]++;
    }

    updateBoard(board); // Update the graphics
} else {
    console.log("Move is blocked.");
}
}

// Check if the move sequence is complete
if (selectedMoves.length === validMoves.length) {
document.querySelectorAll('.point').forEach(point => {
    point.removeEventListener('click', handlePointClick);
});
resolve(selectedMoves); // Resolve the promise with the selected moves
}
}

//Attach click event listeners to all points
document.querySelectorAll('.point').forEach(point => {
point.addEventListener('click', handlePointClick);
});
});
}*/
