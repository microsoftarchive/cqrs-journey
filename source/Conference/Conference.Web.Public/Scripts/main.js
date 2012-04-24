// ==============================================================================================================
// Microsoft patterns & practices
// CQRS Journey project
// ==============================================================================================================
// ©2012 Microsoft. All rights reserved. Certain content used with permission from contributors
// http://cqrsjourney.github.com/contributors/members
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance 
// with the License. You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
// Unless required by applicable law or agreed to in writing, software distributed under the License is 
// distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. 
// See the License for the specific language governing permissions and limitations under the License.
// ==============================================================================================================

$(document).ready(function(){
	$.fn.cycle.defaults.speed   = 900;
	$.fn.cycle.defaults.timeout = 6000;
	
	var RSec = 0;
	var RTile = 0;
	
	$('.tile-slide').each(function(index) {
		$(this).cycle({
			fx:      'scrollDown', 
			speed:    400, 
			timeout:  0
        });
    });
	
	AnimateTile();
	
	function AnimateTile() {
		RSec = Math.floor(Math.random() *5000) + 1000;
		RTile = Math.floor(Math.random() *5);
		setTimeout(function() {						  
			  $('.tile-slide').eq(RTile).cycle('next');
			  AnimateTile();
			}, RSec);
	}
	
	/*
	var RSec = 0;
	$('.tile-slide').each(function(index) {
		RSec = Math.floor(Math.random() *20000) + 3000
		$(this).cycle({
			fx:      'scrollDown', 
			speed:    400, 
			timeout:  RSec
		});
	});
	*/
	
	
	/*var SliderPos = 0;
	var SliderLen = 0;
	var AnimateSpeed = 500;
	var AnimateDelay = AnimateSpeed + 800;
	$('.tile__imgs').each(function(index) {
		$(this).find('img').css({'display': 'none','z-index': 1,'opacity': 0});
		SliderLen = $(this).find('img').length;
		$(this).find('img:first').css({'display': 'block','z-index': SliderLen+1,'opacity': 1});
	});
	
	var hTimer = null;

	$(".tile").mouseenter(function(){
	  var self = this;
	  SliderPos = 0;
	  SliderLen = $(self).find('.tile__imgs>*').length;
	  
	  if(hTimer != null) clearInterval(hTimer);
	  
	  hTimer = setInterval(function() {						  
		  if(SliderLen!=SliderPos+1){
			$(self).find('.tile__imgs>*').eq(SliderPos).css({'display': 'block','z-index': SliderLen});
			$(self).find('.tile__imgs>*').eq(SliderPos+1).css({'display': 'block','z-index': SliderLen+1});
			$(self).find('.tile__imgs>*').eq(SliderPos).animate({'opacity': 0},AnimateSpeed);
			$(self).find('.tile__imgs>*').eq(SliderPos+1).animate({'opacity': 1},AnimateSpeed);
			$(self).find('.tile__imgs>*').eq(SliderPos).css({'display': 'block','z-index': 1});
			SliderPos++;
			} else {
				$(self).find('.tile__imgs>*').eq(SliderPos).css({'display': 'block','z-index': SliderLen});
				$(self).find('.tile__imgs>*').eq(0).css({'display': 'block','z-index': SliderLen+1});
				$(self).find('.tile__imgs>*').eq(SliderPos).animate({'opacity': 0},AnimateSpeed);
				$(self).find('.tile__imgs>*').eq(0).animate({'opacity': 1},AnimateSpeed);
				$(self).find('.tile__imgs>*').eq(SliderPos).css({'display': 'block','z-index': 1});
				SliderPos = 0;
			}
		}, AnimateDelay);				  
	}).mouseleave(function(){
	   if(hTimer != null) {
		 clearInterval(hTimer);
		 hTimer = null;
	   }
	   var self = this;
	   $(self).find('.tile__imgs>*').stop();
	   $(self).find('.tile__imgs>*').css({'display': 'none','z-index': 1,'opacity': 0});
	   $(self).find('.tile__imgs>*:first').css({'display': 'block','z-index': SliderLen+1,'opacity': 1});

	});*/

});

$(function () {
    function getTweets() {
        var $tweets = $("#tweets");
        if ($tweets.length > 0) {
            var user = $tweets.attr("data-user");
            var url = 'http://search.twitter.com/search.json?callback=?&q=' + user;
            $.getJSON(url, function(json) {
                var output = [];
                if (json.results) {
                    for (var i = 0, len = json.results.length; i < len; i++) {

                        //instead of appending each result, add each to the buffer array
                        //output.push('<p><img src="' + json.results[i].profile_image_url + '" widt="48" height="48" />' + json.results[i].text + '</p>');
                        var timeDifference = new Date().getTime() - Date.parse(json.results[i].created_at);
                        var hours = Math.round(timeDifference / (60 * 60 * 1000));
                        output.push('<span class="tile__tweet"><span class="tile__nick"><span class="tile__time">' + hours + 'h ago</span>@' + json.results[i].from_user + '</span>' + json.results[i].text + '</span>');
                    }
                }

                //now select the #results element only once and append all the output at once, then slide it into view
                $("#tweets").html(output.join('')).slideDown('slow');
                $('.tile_twitter .tile-slide').cycle({
                    fx: 'scrollUp',
                    speed: 400,
                    timeout: 0
                });
            });
        }

        setInterval(getTweets, 20000);
    }

    //run the getTweets function on document.ready
    getTweets();
});