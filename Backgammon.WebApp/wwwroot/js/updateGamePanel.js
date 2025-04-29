export function updateGamePanel(gameData, currentMoveIndex) {
    // Assuming 'currentMoveIndex' is the index of the current move being displayed
    // and 'gameData' is an array containing all moves of the game
    if (gameData && gameData.length > 0 && currentMoveIndex >= 0 && currentMoveIndex < gameData.length) {
         // Update the move number displayed to the user
        // Adding 1 because 'currentMoveIndex' is 0-based but we want to show a 1-based count to the user
        document.getElementById('currentMove').textContent = currentMoveIndex + 1;

        // Optionally, you can display additional information about the move here
        // For example, showing player's turn, dice rolled, and move description if available
        const currentMove = gameData[currentMoveIndex];
        //const player = currentMove.player === 0 ? 'Player 1' : 'Player 2'; // Assuming 0 for Player 1, 1 for Player 2
        const diceRolled = `Dice Rolled: ${currentMove.die1}, ${currentMove.die2}`;
        const moveDescription = currentMove.move?.moveAnnotation ?? '';
        const scoreVectorDescription = currentMove.scoreEstimation || '';
        // Update the game status with more detailed information
        // Update current move
        document.getElementById('currentMove').textContent = currentMoveIndex + 1;

        // Update dice rolled (assuming diceRolled is a string or number)
        document.getElementById('diceRolled').textContent = diceRolled;

        // Update move details (assuming moveDescription is a string)
        document.getElementById('moveDetails').textContent = moveDescription;
        document.getElementById('scoreEstimation').textContent = scoreVectorDescription;
    } else {
        // Handle cases where the game data is not loaded or the index is out of bounds
        document.getElementById('currentMove').textContent = 'N/A';
        // Clear additional information since there's no move to display
        document.getElementById('gameStatus').innerHTML = `<p>Move: <span id="currentMove">N/A</span></p>`;
    }
}