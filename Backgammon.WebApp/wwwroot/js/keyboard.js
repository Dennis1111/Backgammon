export function registerUndoShortcut(dotnetHelper) {
    document.addEventListener('keydown', function (e) {
        if (e.ctrlKey && e.key === 'z') {
            dotnetHelper.invokeMethodAsync('UndoMoveFromJs');
        }
    });
}