(() => {
    document.querySelectorAll('[data-meal-delete]').forEach(button => button.addEventListener('click', () => {
        document.querySelector('[data-delete-meal-id]').value = button.dataset.mealDelete;
        document.querySelector('[data-delete-meal-caption]').textContent = button.dataset.mealName;
    }));
    document.querySelectorAll('[data-recipe-name]').forEach(button => button.addEventListener('click', () => {
        document.getElementById('Meal_Name').value = button.dataset.recipeName;
    }));
    document.querySelectorAll('[data-plan-name]').forEach(button => button.addEventListener('click', () => { document.getElementById('Goals_Title').value = button.dataset.planName; }));
    const errors = document.querySelector('.validation-summary-errors');
    if (errors) errors.scrollIntoView({ block: 'center' });
})();

