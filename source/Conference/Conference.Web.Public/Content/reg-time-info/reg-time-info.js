$(function() {
  var w = $(window);
  var rti = $('.reg-time-info');
	var currentScrollTop = 0
	w.scroll(function(){ 
		currentScrollTop = w.scrollTop() - 285;
		rti.css('top', currentScrollTop > 0 ? currentScrollTop : 0);
	});
});