export function scrollTableBodyToBottom(element) {
    if (element) {
        element.scrollTop = element.scrollHeight;
    }
}