$(function() {
	$('.js-reg-form').click(function(){
		$('.tabs__item').removeClass('tabs__item_active');
		$(this).addClass('tabs__item_active');
		
		$('.content').hide();
		$('.content_reg').show();
		return false;
	});
	$('.js-login-form').click(function(){
		$('.tabs__item').removeClass('tabs__item_active');
		$(this).addClass('tabs__item_active');
		
		$('.content').hide();
		$('.content_login').show();
		return false;
	});
});