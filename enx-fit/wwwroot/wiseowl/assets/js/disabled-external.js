(function (window) {
  "use strict";

  var noop = function () {};
  var chainable = function () { return this; };

  window.Pace = window.Pace || { on: noop, restart: noop, stop: noop };
  window.Modernizr = window.Modernizr || {};
  window.Popper = window.Popper || { createPopper: function () { return { destroy: noop, update: noop }; } };
  window.CountUp = window.CountUp || function () { return { start: noop, update: noop, reset: noop }; };
  window.Switchery = window.Switchery || function () {};
  window.Chart = window.Chart || function () { return { destroy: noop, update: noop, render: noop }; };
  window.m = window.m || function () { return null; };
  window.m.mount = window.m.mount || noop;
  window.m.request = window.m.request || function () { return Promise.resolve([]); };
  window.moment = window.moment || function () {
    return {
      format: function () { return ""; },
      subtract: function () { return this; },
      add: function () { return this; },
      startOf: function () { return this; },
      endOf: function () { return this; }
    };
  };
  window.swal = window.swal || function () { return Promise.resolve(); };
  window.toastr = window.toastr || { info: noop, success: noop, warning: noop, error: noop, options: {} };
  window.tinymce = window.tinymce || { init: noop };

  function installJQueryStubs() {
    var $ = window.jQuery || window.$;
    if (!$ || !$.fn) return;

    [
      "metisMenu",
      "perfectScrollbar",
      "circleProgress",
      "sparkline",
      "daterangepicker",
      "datepicker",
      "clockpicker",
      "colorpicker",
      "slick",
      "magnificPopup",
      "fullCalendar",
      "footable",
      "DataTable",
      "dataTable",
      "bootstrapTable",
      "editableTableWidget",
      "vectorMap",
      "gmap3",
      "inputmask",
      "dropify",
      "dropzone",
      "select2",
      "TouchSpin",
      "multiSelect",
      "tinymce",
      "wysihtml5",
      "nestedSortable",
      "sortable",
      "mediaelementplayer",
      "nestable",
      "toast"
    ].forEach(function (name) {
      if (!$.fn[name]) $.fn[name] = chainable;
    });

    $.plot = $.plot || noop;
  }

  installJQueryStubs();
  window.__installDisabledExternalPlugins = installJQueryStubs;
})(window);
