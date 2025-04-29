import { debounce } from './uiHelpers.js';

export function initializeGameControls(updateBoard, updateGamePanel) {
    let currentMoveIndex = 0;
    let gameData = [];

    const loadGameButton = document.getElementById('loadGame');
    const prevMoveButton = document.getElementById('prevMove');
    const nextMoveButton = document.getElementById('nextMove');
    const currentMoveDisplay = document.getElementById('currentMove');

    if (!loadGameButton || !prevMoveButton || !nextMoveButton || !currentMoveDisplay) {
        console.error("Game controls not found in the DOM.");
        return;
    }

    loadGameButton.addEventListener('click', () => {
        fetch('api/game/generate-game')
            .then(response => response.json())
            .then(data => {
                console.log("data loaded", data);
                gameData = data;
                console.log("Game data loaded", gameData);
                currentMoveIndex = 0;
                let board = gameData[currentMoveIndex].board;
                let die1 = gameData[currentMoveIndex].die1;
                let die2 = gameData[currentMoveIndex].die2;
                let player = gameData[currentMoveIndex].player;
                updateBoard(board, die1,die2, player);
                updateGamePanel(gameData, currentMoveIndex);
            })
            .catch(error => console.error('Error loading game:', error));
    });

    prevMoveButton.addEventListener('click', () => {
        if (gameData.length > 0 && currentMoveIndex > 0) {
            currentMoveIndex--;
            let board = gameData[currentMoveIndex].board;
            let die1 = gameData[currentMoveIndex].die1;
            let die2 = gameData[currentMoveIndex].die2;
            let player = gameData[currentMoveIndex].player;
            updateBoard(board, die1, die2, player);
            updateGamePanel(gameData, currentMoveIndex);
        }
    });

    nextMoveButton.addEventListener('click', () => {
        if (gameData.length > 0 && currentMoveIndex < gameData.length - 1) {
            currentMoveIndex++;
            let board = gameData[currentMoveIndex].board;
            let die1 = gameData[currentMoveIndex].die1;
            let die2 = gameData[currentMoveIndex].die2;
            let player = gameData[currentMoveIndex].player;
            updateBoard(board, die1, die2, player);
            updateGamePanel(gameData , currentMoveIndex);
        }
    });

    window.addEventListener('resize', debounce(() => {
        if (gameData.length > 0) {
            const { board, die1, die2, player } = gameData[currentMoveIndex];
            updateBoard(board, die1, die2, player);
        }
    }, 150));
}
