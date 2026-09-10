export function queuetooltip(node: HTMLElement, text: string) {
    const tooltipElement = document.createElement('div');
    tooltipElement.textContent = text;
    tooltipElement.setAttribute('role', 'tooltip');

    Object.assign(tooltipElement.style, {
        position: 'absolute',
        bottom: 'calc(100% + 10px)',
        left: '50%',
        transform: 'translateX(-50%)',
        maxWidth: '280px',
        padding: '0.5rem 0.75rem',
        border: '2px solid #686868',
        borderRadius: '0.5rem',
        background: '#fff8e8',
        boxShadow: '3px 3px #686868',
        color: '#3f3f3f',
        fontSize: '0.85rem',
        fontWeight: '600',
        lineHeight: '1.3',
        textAlign: 'center',
        zIndex: '1000',
        pointerEvents: 'none',
        whiteSpace: 'normal'
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
    node.addEventListener('focus', handleMouseEnter);
    node.addEventListener('blur', handleMouseLeave);

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
            node.removeEventListener('focus', handleMouseEnter);
            node.removeEventListener('blur', handleMouseLeave);
        }
    };
}
