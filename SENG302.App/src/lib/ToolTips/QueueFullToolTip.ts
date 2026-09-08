export function queuetooltip(node: HTMLSpanElement, text: string) {
    let tooltipElement : HTMLDivElement;

    function handleMouseEnter() {
        // Create the tooltip element dynamically
        tooltipElement = document.createElement('div');
        tooltipElement.textContent = text;

        // Apply basic styling
        Object.assign(tooltipElement.style, {
            position: 'absolute',
            background: '#333',
            color: '#fff',
            padding: '5px 10px',
            borderRadius: '4px',
            fontSize: '12px',
            left: "0%",
            zIndex: '1000',
            pointerEvents: 'none',
            whiteSpace: 'nowrap'
        });

        node.appendChild(tooltipElement);
        
    }

    function handleMouseLeave() {
        if (tooltipElement) {
            tooltipElement.remove();
        }
    }

    // Attach event listeners
    node.addEventListener('mouseenter', handleMouseEnter);
    node.addEventListener('mouseleave', handleMouseLeave);

    return {
        // Update text if it changes reactively
        update(newText: string) {
            text = newText;
            if (tooltipElement) tooltipElement.textContent = text;
        },
        // Clean up DOM on element destruction
        destroy() {
            handleMouseLeave();
            node.removeEventListener('mouseenter', handleMouseEnter);
            node.removeEventListener('mouseleave', handleMouseLeave);
        }
    };
}
