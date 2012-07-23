// ==============================================================================================================
// Microsoft patterns & practices
// CQRS Journey project
// ==============================================================================================================
// ©2012 Microsoft. All rights reserved. Certain content used with permission from contributors
// http://go.microsoft.com/fwlink/p/?LinkID=258575
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance 
// with the License. You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
// Unless required by applicable law or agreed to in writing, software distributed under the License is 
// distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. 
// See the License for the specific language governing permissions and limitations under the License.
// ==============================================================================================================

$.fn.jsSelect = function() {
	var JsSelect__template = '<div class="form-select-pad">' +
		'<div class="form-select js-select">' +
			'<span class="form-select__drop-down"><span></span></span>' +
			'<div class="form-select__active js-current"></div>' +
			'<div class="form-select__options js-options" style="display: none;"><ul class="form-select__options"></ul></div>' +
		'</div>' +	
		'<input class="js-input" type="hidden"/>' +
	'</div>';
	
	
	var $cont = null;
	var valueLabels = {};
	var isOptionsOpen = false;
	
	var init = function($select) {
		$cont = $(JsSelect__template);	
		initClassAndName($select);
		initOptions($select);
		setValue($select.val());
		$select.replaceWith($cont);
		//handlers
		$cont.find('.js-select').click(onClickSelect);
		$cont.find('.js-options li.form-select__options__item').click(onClickOption);
		$(document.body).click(onBodyClick);
	};
	
	var initClassAndName = function($select) {
		if($select[0].className) {
			$cont.addClass($select[0].className);
		}
		$cont.find('.js-input').attr('name', $select.attr('name'));
	};
	
	var initOptions = function($select) {
		var options = $select[0].options;
		$cont.find('.js-options ul').append('<li class="form-select__options__active-item js-current"></li>');
		for(var i=0; i<options.length; i++) {
			var option = options[i];
			valueLabels[option.value] = option.text;
			
			var $opt = $('<li class="form-select__options__item"/>');
			$opt.text(option.text);
			$opt.data('val', option.value);
			if(option.value == $select.val())
				$opt.show();
			$cont.find('.js-options ul').append($opt);
		}
	};
	
	var setValue = function(value) {
		var label = valueLabels[value];
		$cont.find('.js-current').text(label);
		$cont.find('.js-input').val(value).change();
	};
	
	var onClickSelect = function() {
		if(!isOptionsOpen) {
			$cont.find('.js-options').show();
			isOptionsOpen = true;
			return false;
		}
	};
	
	var onBodyClick = function() {
		if(isOptionsOpen) {
			$cont.find('.js-options').hide();
			isOptionsOpen = false;
		}
	};
	
	var onClickOption = function() {
		setValue($(this).data('val'));
		$('.form-select__options__item').show();
	};
	
	init(this);
};


$(function() {
	$('select').each(function(){
		$(this).jsSelect();
	});

    $(window).bind('form-reload', function(e, data) {
        data.$form.find('select').each(function(){
            $(this).jsSelect();
        });
    });
	
	$('.js-radiobutton input').change(function(){
		$('.js-radiobutton-box').hide();
		$('.nav__right-small').hide();
		$('.js-radiobutton input:checked').each(function () {
		    var name = $(this).parent().attr('name');
		    $('.' + name + '-box').show();
		    $('.' + name + '-proceed').show();
		});
	});
	
	$('.js-checkbox').click(function(){
		$(this).toggleClass('form__chb__item_a');
	});
	
	$('.js-checkbox-seats').click(function(){
		$(this).toggleClass('form__chb__item_a');
		$('.form__seats-select').toggle();
	});
	
	$('.j-promocode-field').click(function(){
		$(this).hide();
		$('.form-promo').show();
		return false;
	});
});