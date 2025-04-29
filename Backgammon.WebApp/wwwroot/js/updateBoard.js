export function updateBoard(board, die1, die2, player) {
    
    clearBoard(board);
    adjustCheckerSize();

    for (let i = 0; i < board.length; i++) {
        const pointId = `point-${i}`;
        const numberOfCheckers = Math.abs(board[i]);
        const playerClass = board[i] > 0 ? 'player1' : 'player2';
        if (numberOfCheckers > 0) {
            if (i == 26 || i == 27) {
                placeStandingCheckersInBearOff(pointId, numberOfCheckers, playerClass);
            } else {
                placeCheckers(pointId, numberOfCheckers, playerClass);
            }
        }
    }

    let diceValues = [die1, die2];
    showDice(player, diceValues);
    document.querySelector('.dice-container').classList.remove('hidden');
}

function clearBoard(board) {
    const boardLength = board?.length || 0;
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

function placeCheckers(pointId, numberOfCheckers, player) {
    const pointElement = document.getElementById(pointId);
    pointElement.innerHTML = ''; // Clear existing checkers
    const checkerSize = parseFloat(getComputedStyle(document.documentElement).getPropertyValue('--checker-size'));

    // A little smaller size but should probably be related to the later code instead
    const stackedCheckerSize = checkerSize - 2;
    const stackStart = 10; // Initial bottom offset
    // const overlap = 0; // How much checkers overlap when stacked beyond 5

    for (let i = 0; i < numberOfCheckers; i++) {
        const checker = document.createElement('div');
        checker.style.width = checkerSize + 'px';
        checker.style.height = checkerSize + 'px';
        checker.classList.add('checker', player); // Add 'checker' class and either 'white' or 'black'

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

function placeStandingCheckersInBearOff(pointId, numberOfCheckers, player) {
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
            checker.classList.add('bear-off-checker', player); // Use 'bear-off-checker' class for specific styling
            checker.style.top = `${yOffset}px`; // Stack checkers from the top down
            pointElement.appendChild(checker);
            yOffset += 13; // Increment yOffset for the next checker
        }
    } else {
        yOffset = 10; // Adjust if needed based on the height of the bear-off area
        //const bearOffHeight = pointElement.offsetHeight; // Get the height of the bear-off container
        for (let i = 0; i < numberOfCheckers; i++) {
            const checker = document.createElement('div');
            checker.style.width = checkerSize + 'px';
            checker.style.height = (checkerSize * 0.2) + 'px';
            checker.classList.add('bear-off-checker', player); // Use 'bear-off-checker' class for specific styling
            checker.style.bottom = `${(numberOfCheckers - i - 1) * 13 + yOffset}px`; // Stack checkers from the bottom up
            pointElement.appendChild(checker);
        }
    }
}    

function createDiceDots(value) {
    const dotPositions = {
        1: [5],
        2: [1, 9],
        3: [1, 5, 9],
        4: [1, 3, 7, 9],
        5: [1, 3, 5, 7, 9],
        6: [1, 3, 4, 6, 7, 9]
    };

    const grid = document.createElement('div');
    grid.className = 'dice-grid';
    grid.dataset.value = value;

    for (let i = 1; i <= 9; i++) {
        const dot = document.createElement('div');
        if (dotPositions[value].includes(i)) {
            dot.className = 'dice-dot';
        }
        grid.appendChild(dot);
    }

    return grid;
}


function showDice(player, diceValues) {
    const diceContainer = document.querySelector('.dice-container');
    diceContainer.style.right = player === 0 ? '20%' : 'auto';
    diceContainer.style.left = player === 0 ? 'auto' : '20%';

    // Clear existing dice
    diceContainer.innerHTML = '';

    diceValues.forEach(value => {
        const dice = createDiceDots(value);
        dice.classList.add(player === 0 ? 'dice-player1' : 'dice-player2');
        dice.dataset.value = value; // Set the dice value in a data attribute
        diceContainer.appendChild(dice);
    });
}



/*function showDice(player, diceValues) {
    const diceContainer = document.querySelector('.dice-container');

    // Adjust the dice container's position based on the player
    diceContainer.style.right = player === 0 ? '20%' : 'auto';
    diceContainer.style.left = player === 0 ? 'auto' : '20%';

    // Dice unicode values for each side of the die
    const diceUnicode = ['⚀', '⚁', '⚂', '⚃', '⚄', '⚅'];

    // Get the player's dice elements
    const dicePlayer1 = document.querySelectorAll('.dice-player1');
    const dicePlayer2 = document.querySelectorAll('.dice-player2');

    // Hide all dice first
    [...dicePlayer1, ...dicePlayer2].forEach(dice => dice.style.display = 'none');

    // Select the active dice container based on the player
    const activeDice = player === 0 ? dicePlayer1 : dicePlayer2;

    // Calculate dice size based on window size
    const diceSize = Math.min(window.innerWidth, window.innerHeight) * 0.1; // Adjust dice size as 10% of the smallest dimension

    // Apply the calculated size to each dice element
    activeDice.forEach((dice, index) => {
        if (index < diceValues.length) {
            const value = diceValues[index];
            dice.textContent = diceUnicode[value - 1] || ''; // Set the correct Unicode character for the dice value
            dice.style.display = 'block'; // Show the dice
            dice.style.fontSize = `${diceSize * 0.6}px`; // Adjust font size to match the dice size (60% of dice size)
            dice.style.width = `${diceSize}px`; // Set the width based on calculated dice size
            dice.style.height = `${diceSize}px`; // Set the height based on calculated dice size
            dice.style.lineHeight = `${diceSize}px`; // Vertically center the dice text within the box
            dice.style.textAlign = 'center'; // Ensure text is centered horizontally
        }
    });
}*/

/*
function showDice(player, diceValues) {
    const diceContainer = document.querySelector('.dice-container');
    diceContainer.style.right = player === 0 ? '20%' : 'auto';
    diceContainer.style.left = player === 0 ? 'auto' : '20%';

    const diceUnicode = ['⚀', '⚁', '⚂', '⚃', '⚄', '⚅'];

    const dicePlayer1 = document.querySelectorAll('.dice-player1');
    const dicePlayer2 = document.querySelectorAll('.dice-player2');

    // Hide all dice first
    [...dicePlayer1, ...dicePlayer2].forEach(dice => dice.style.display = 'none');

    const activeDice = player === 0 ? dicePlayer1 : dicePlayer2;
    activeDice.forEach((dice, index) => {
        if (index < diceValues.length) {
            const value = diceValues[index];
            dice.textContent = diceUnicode[value - 1] || ''; // Ensure valid index
            dice.style.display = 'block';
        }
    });
}*/

/*function showDice(player, diceValues) {
    const diceContainer = document.querySelector('.dice-container');
    diceContainer.style.right = player === 0 ? '20%' : 'auto';
    diceContainer.style.left = player === 0 ? 'auto' : '20%';

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
}*/

export function adjustCheckerSize() {
    const board = document.querySelector('.board');
    if (board) {
        const boardWidth = board.offsetWidth; // Get the current width of the board
        const checkerSize = boardWidth * 0.05; // Calculate checker size as 5% of board width
        console.log("set size", checkerSize);
        // Set CSS variables on root or board level
        document.documentElement.style.setProperty('--checker-size', `${checkerSize}px`);
    }
}