export function queuetooltip(node: HTMLSpanElement, text: string) {
    const tooltipElement = document.createElement('div');
    tooltipElement.textContent = text;

    Object.assign(tooltipElement.style, {
        position: 'absolute',
        background: '#333',
        color: '#fff',
        padding: '5px 1px',
        borderRadius: '4px',
        fontSize: '12px',
        left: '50%',
        zIndex: '1000',
        pointerEvents: 'none',
        whiteSpace: 'nowrap'
    });

    function handleMouseEnter() {
        if (!tooltipElement.isConnected) {
            node.appendChild(tooltipElement);
        }
    }

    function handleMouseLeave() {
        tooltipElement.remove();
    }

    // Attach event listeners
    node.addEventListener('mouseenter', handleMouseEnter);
    node.addEventListener('mouseleave', handleMouseLeave);

    return {
        // Update text if it changes reactively
        update(newText: string) {
            text = newText;
            tooltipElement.textContent = text;
        },
        // Clean up DOM on element destruction
        destroy() {
            handleMouseLeave();
            node.removeEventListener('mouseenter', handleMouseEnter);
            node.removeEventListener('mouseleave', handleMouseLeave);
        }
    };
}
