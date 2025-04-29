import { initializeGameControls } from './viewGameControls.js';
import { updateBoard } from './updateBoard.js';
import { updateGamePanel } from './updateGamePanel.js';

export function initializeGame() {
    initializeGameControls(updateBoard, updateGamePanel);
}