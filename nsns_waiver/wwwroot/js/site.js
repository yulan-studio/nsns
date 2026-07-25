(() => {
    const container = document.querySelector("#family-members");
    const template = document.querySelector("#family-member-template");
    const addButton = document.querySelector("#add-family-member");
    const status = document.querySelector("#family-member-status");

    if (!container || !template || !addButton || !status) {
        return;
    }

    const maximum = Number.parseInt(container.dataset.maximum ?? "10", 10);
    const fields = ["FirstName", "LastName", "Relationship"];

    function configureField(card, field, index) {
        const input = card.querySelector(
            `[data-field="${field}"], input[name$=".${field}"]`);

        if (!input) {
            return;
        }

        const fullName = `Input.FamilyMembers[${index}].${field}`;
        const id = `Input_FamilyMembers_${index}__${field}`;
        input.name = fullName;
        input.id = id;

        if (field !== "Relationship") {
            input.required = true;
        }

        const label = card.querySelector(
            `[data-field-label="${field}"], label[for$="__${field}"]`);
        if (label) {
            label.htmlFor = id;
        }

        const validation = card.querySelector(
            `[data-validation="${field}"], [data-valmsg-for$=".${field}"]`);
        if (validation) {
            validation.dataset.valmsgFor = fullName;
        }
    }

    function reindex() {
        const cards = [...container.querySelectorAll("[data-family-member]")];

        cards.forEach((card, index) => {
            const number = card.querySelector("[data-family-number]");
            if (number) {
                number.textContent = String(index + 1);
            }

            fields.forEach(field => configureField(card, field, index));
        });

        status.textContent = `${cards.length} of ${maximum} family members added.`;
        addButton.disabled = cards.length >= maximum;
    }

    addButton.addEventListener("click", () => {
        if (container.querySelectorAll("[data-family-member]").length >= maximum) {
            return;
        }

        const fragment = template.content.cloneNode(true);
        container.appendChild(fragment);
        reindex();

        const cards = container.querySelectorAll("[data-family-member]");
        cards[cards.length - 1]?.querySelector("input")?.focus();
    });

    container.addEventListener("click", event => {
        const removeButton = event.target.closest("[data-remove-family]");
        if (!removeButton) {
            return;
        }

        removeButton.closest("[data-family-member]")?.remove();
        reindex();
    });

    reindex();
})();
