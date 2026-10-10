(() => {
  'use strict';
  const noticeStates = new Set(['info','saved','local','pending','error']);
  // States are supplied by the owning workflow; never infer persistence from message text.
  window.AppComponents = Object.freeze({
    setNoticeState(element, state) {
      if (!element) return;
      element.dataset.appState = noticeStates.has(state) ? state : 'info';
    }
  });
})();
