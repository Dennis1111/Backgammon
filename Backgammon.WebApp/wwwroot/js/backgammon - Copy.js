function initializeGameControls() {
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

    loadGameButton.addEventListener('click', function () {
        fetch('api/game/generate-game')
            .then(response => response.json())
            .then(data => {
                gameData = data;
                currentMoveIndex = 0;
                updateBoard();
            })
            .catch(error => console.error('Error loading game:', error));
    });

    prevMoveButton.addEventListener('click', function () {
        if (currentMoveIndex > 0) {
            currentMoveIndex -= 1;
            updateBoard();
        }
    });

    nextMoveButton.addEventListener('click', function () {
        if (currentMoveIndex < gameData.length - 1) {
            currentMoveIndex += 1;
            updateBoard();
        }
    });

    function updateBoard() {
        clearBoard();
        if (gameData && currentMoveIndex >= 0 && currentMoveIndex < gameData.length) {
            const currentMove = gameData[currentMoveIndex];
            const board = currentMove.board;
            console.log("board", board);

            for (let i = 0; i < board.length; i++) {
                const pointId = `point-${i}`;
                const numberOfCheckers = Math.abs(board[i]);
                const checkerColor = board[i] > 0 ? 'white' : 'black';
                if (numberOfCheckers > 0) {
                    if (i == 26 || i == 27) {
                        placeStandingCheckersInBearOff(pointId, numberOfCheckers, checkerColor);
                    } else {
                        placeCheckers(pointId, numberOfCheckers, checkerColor);
                    }
                }
            }

            die1 = currentMove.die1;
            die2 = currentMove.die2;
            let diceValues = [die1, die2];
            let player = currentMove.player;
            showDice(player, diceValues);
            showMove();
            document.querySelector('.dice-container').classList.remove('hidden');
        } else {
            console.error("Invalid move index or game data not loaded");
        }
    }

    function clearBoard() {
        const boardLength = gameData[currentMoveIndex]?.board?.length || 0;
        for (let i = 0; i < boardLength; i++) {
            const pointId = `point-${i}`;
            clearCheckers(pointId);
        }
    }

    function clearCheckers(pointId) {
        const pointElement = document.getElementById(pointId);
        if (pointElement) {
            const checkers = pointElement.querySelectorAll('.checker, .bear-off-checker');
            checkers.forEach(checker => pointElement.removeChild(checker));
        }
    }

    function placeCheckers(pointId, numberOfCheckers, color) {
        const pointElement = document.getElementById(pointId);
        pointElement.innerHTML = ''; // Clear existing checkers
        const checkerSize = parseFloat(getComputedStyle(document.documentElement).getPropertyValue('--checker-size'));

        //const checkerSize = 40; // Size of checker, matches CSS
        // A little smaller size but should probably be related to the later code instead
        // checker.style.width = `${checkerSize - 2}px`; // Slightly smaller
        const stackedCheckerSize = checkerSize - 2;
        const stackStart = 10; // Initial bottom offset
        const overlap = 0; // How much checkers overlap when stacked beyond 5

        for (let i = 0; i < numberOfCheckers; i++) {
            const checker = document.createElement('div');
            checker.style.width = checkerSize + 'px';
            checker.style.height = checkerSize + 'px';
            checker.classList.add('checker', color); // Add 'checker' class and either 'white' or 'black'

            if (i < 5) {
                checker.style.bottom = `${stackStart + (i * (checkerSize + 1))}px`; // Adjust for slight overlap
            } else {
                // Indicate more than 5 checkers by adjusting the style
                checker.style.width = `${checkerSize - 2}px`; // Slightly smaller
                checker.style.height = `${checkerSize - 2}px`; // Slightly smaller
                const y = `${stackStart + 15 + ((i - 5) * (stackedCheckerSize + 1))}px`;
                checker.style.bottom = y;
            }

            // Adjust position for upper half
            if (pointElement.parentElement.classList.contains('upper-half')) {
                checker.style.top = checker.style.bottom;
                delete checker.style.bottom;
            }

            pointElement.appendChild(checker);
        }
    }

    function placeStandingCheckersInBearOff(pointId, numberOfCheckers, color) {
        const pointElement = document.getElementById(pointId);
        const checkerSize = parseFloat(getComputedStyle(document.documentElement).getPropertyValue('--checker-size'));
        let yOffset; // Variable for the offset initialization

        // Adjust starting position based on top or bottom bear-off area
        if (pointElement.classList.contains('bear-off-top')) {
            yOffset = 10; // Starting from the top for the top bear-off
            for (let i = 0; i < numberOfCheckers; i++) {
                const checker = document.createElement('div');
                checker.style.width = checkerSize + 'px';
                checker.style.height = (checkerSize * 0.2) + 'px';
                checker.classList.add('bear-off-checker', color); // Use 'bear-off-checker' class for specific styling
                checker.style.top = `${yOffset}px`; // Stack checkers from the top down
                pointElement.appendChild(checker);
                yOffset += 13; // Increment yOffset for the next checker
            }
        } else {
            yOffset = 10; // Adjust if needed based on the height of the bear-off area
            const bearOffHeight = pointElement.offsetHeight; // Get the height of the bear-off container
            for (let i = 0; i < numberOfCheckers; i++) {
                const checker = document.createElement('div');
                checker.style.width = checkerSize + 'px';
                checker.style.height = (checkerSize * 0.2) + 'px';
                checker.classList.add('bear-off-checker', color); // Use 'bear-off-checker' class for specific styling
                checker.style.bottom = `${(numberOfCheckers - i - 1) * 13 + yOffset}px`; // Stack checkers from the bottom up
                pointElement.appendChild(checker);
            }
        }   
    }

    function showMove() {
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
            /*document.getElementById('gameStatus').innerHTML = `
                <p>Move: <span id="currentMove">${currentMoveIndex + 1}</span> - ${player}</p>
                <p>${diceRolled}</p>
                <p>Move Details: ${moveDescription}</p>
            `;*/
        } else {
            // Handle cases where the game data is not loaded or the index is out of bounds
            document.getElementById('currentMove').textContent = 'N/A';
            // Clear additional information since there's no move to display
            document.getElementById('gameStatus').innerHTML = `<p>Move: <span id="currentMove">N/A</span></p>`;
        }
    }

    function showDice(player, diceValues) {
        const diceContainer = document.querySelector('.dice-container');
        diceContainer.style.right = player === 0 ? '20%' : 'auto';
        diceContainer.style.left = player === 0 ?  'auto': '20%' ;

        /*       // Clear previous dice classes for fresh update
               document.querySelectorAll('.dice').forEach(dice => {
               dice.style.display = 'none'; // Hide all first
               dice.className = 'dice'; // Reset class name to just 'dice'
            });
        
            // Show dice based on values
            diceValues.forEach((value, index) => {
                const diceClass = ['one', 'two', 'three', 'four', 'five', 'six'][value - 1]; // Mapping value to class
                const dice = player === 0 ? document.querySelector(`#dice${index + 1}-player1`) : document.querySelector(`#dice${index + 1}-player2`);
                if (dice) {
                    dice.classList.add(diceClass); // Add class based on dice value
                    dice.style.display = 'block'; // Show dice
                }
            });*/

        // Update the container's side based on the player

        // Assuming you have separate dice elements for each player
        const dicePlayer1 = document.querySelectorAll('.dice-player1');
        const dicePlayer2 = document.querySelectorAll('.dice-player2');

        // Hide all dice first
        [...dicePlayer1, ...dicePlayer2].forEach(dice => dice.style.display = 'none');

        // Display dice based on the player and update values
        if (player === 0) { // Player 1
            dicePlayer1.forEach((dice, index) => {
                if (index < diceValues.length) {
                    dice.textContent = diceValues[index]; // Update value
                    dice.style.display = 'block'; // Show dice
                }
            });
        } else { // Player 2
            dicePlayer2.forEach((dice, index) => {
                if (index < diceValues.length) {
                    dice.textContent = diceValues[index]; // Update value
                    dice.style.display = 'block'; // Show dice
                }
            });
        }
    }

    function adjustCheckerSize() {
        const board = document.querySelector('.board');
        if (board) {
            const boardWidth = board.offsetWidth; // Get the current width of the board
            const checkerSize = boardWidth * 0.05; // Calculate checker size as 5% of board width
            console.log("set size", checkerSize);
            // Set CSS variables on root or board level
            document.documentElement.style.setProperty('--checker-size', `${checkerSize}px`);
            document.documentElement.style.setProperty('--checker-size', `${checkerSize}px`);
        }

        // updateBoard();
    }

    // Adjust checker size on page load
    adjustCheckerSize();

    // Optionally, adjust checker size on window resize with debounce to improve performance
    window.addEventListener('resize', debounce(adjustCheckerSize, 250));

    // Debounce function to limit the rate at which a function is executed
    function debounce(func, wait, immediate) {
        var timeout;
        return function () {
            var context = this, args = arguments;
            var later = function () {
                timeout = null;
                if (!immediate) func.apply(context, args);
            };
            var callNow = immediate && !timeout;
            clearTimeout(timeout);
            timeout = setTimeout(later, wait);
            if (callNow) func.apply(context, args);
        };
    }
}
