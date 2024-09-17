function scrollToElement(elementId) {
    const element = document.getElementById(elementId);
    if (element instanceof HTMLElement) {
        element.scrollIntoView({
            behavior: "smooth",
            block: "center",
            inline: "center"
        });
    }
}
