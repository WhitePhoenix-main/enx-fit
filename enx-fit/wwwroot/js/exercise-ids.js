(() => {
    'use strict';
    // Match ExerciseIds.FromLegacy and the database migration for old drafts and favorites.
    const normalize = value => {
        if (typeof value === 'string' && /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i.test(value)) return value.toLowerCase();
        const legacy = typeof value === 'number' ? value : typeof value === 'string' && /^-?\d+$/.test(value) ? Number(value) : NaN;
        if (!Number.isInteger(legacy) || legacy < -2147483648 || legacy > 2147483647 || legacy === 0) return null;
        return 'e71c15e5-0000-4000-8000-0000' + (legacy >>> 0).toString(16).padStart(8, '0');
    };
    window.ExerciseIds = { normalize };
})();
