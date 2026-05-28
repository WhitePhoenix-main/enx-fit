$(function() {
  const $openModalButtons = $('.request-loader');
  const $overlay = $('#modal-overlay');
  const $closeModal = $('.my-close');
  const $videoFrame = $('#my-video-frame');

  function getSafeVideoUrl(rawUrl) {
      try {
          const url = new URL(rawUrl, window.location.origin);
          const isSameOrigin = url.origin === window.location.origin;

          if (!isSameOrigin) {
              return null;
          }

          url.searchParams.set('autoplay', '1');
          return url.toString();
      } catch {
          return null;
      }
  }

  $openModalButtons.on('click', function() {
      const videoUrl = getSafeVideoUrl($(this).attr('data-video'));
      if (!videoUrl) {
          return;
      }

      $videoFrame.attr('src', videoUrl);
      $overlay.css('display', 'flex');
  });

  $closeModal.on('click', function() {
      $overlay.hide();
      $videoFrame.attr('src', "");
  });

  $overlay.on('click', function(e) {
      if (e.target === this) {
          $overlay.hide();
          $videoFrame.attr('src', "");
      }
  });
});
