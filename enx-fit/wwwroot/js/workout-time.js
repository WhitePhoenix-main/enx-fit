(() => {
    'use strict';
    document.cookie = `enix-utc-offset=${-new Date().getTimezoneOffset()}; Path=/; SameSite=Lax; Max-Age=31536000`;
})();
