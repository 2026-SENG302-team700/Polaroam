/**
 * formats the string based on the users locale
 */
export function formatDate(dateString: string, includeTime: boolean = false) {
    const date = new Date(dateString);
    if (includeTime) {
        return date.toLocaleString("en-NZ");
    }
    return date.toLocaleDateString("en-NZ");
}