/**
 * formats the string based on the users locale
 */
export function formatDate(dateString: string, includeTime: boolean = false) {
    const date = new Date(dateString);
    if (includeTime) {
        return date.toLocaleString();
    }
    return date.toLocaleDateString();
}

/**
 * Serialises a calendar date without allowing the users timezone to shift it
 * onto the wrong day.
 */
export function serializeLocalDate(date: Date): string {
    return new Date(
        Date.UTC(date.getFullYear(), date.getMonth(), date.getDate(), 12),
    ).toISOString();
}
