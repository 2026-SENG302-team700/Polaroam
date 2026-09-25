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
    const pad = (value: number) => String(value).padStart(2, "0");
    const offsetMinutes = -date.getTimezoneOffset();
    const offsetSign = offsetMinutes >= 0 ? "+" : "-";
    const absoluteOffset = Math.abs(offsetMinutes);
    const offsetHours = pad(Math.floor(absoluteOffset / 60));
    const offsetRemainder = pad(absoluteOffset % 60);

    return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}T12:00:00${offsetSign}${offsetHours}:${offsetRemainder}`;
}
