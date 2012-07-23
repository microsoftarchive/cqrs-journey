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

$(function() {
  var w = $(window);
  var rti = $('.reg-time-info');
    var currentScrollTop = 0;
	w.scroll(function(){ 
		currentScrollTop = w.scrollTop() - 285;
		rti.css('top', currentScrollTop > 0 ? currentScrollTop : 0);
    });


	var Conference = {};

    Conference.StartTimer = function(element, timeoutCallback, formatCallback) {
        timeoutCallback = (typeof timeoutCallback === 'undefined') ? Conference._DefaultTimeoutCallback(element) : timeoutCallback;
        formatCallback = (typeof formatCallback === 'undefined') ? Conference._DefaultFormatCallback : formatCallback;

        var targetDate = new Date(parseInt(element.getAttribute('data-targetDate')));

        var timerCallback = function() {
            var formattedMilliseconds = '';
            var currentDate = new Date();

            var dateDiff = targetDate.getTime() - currentDate.getTime();
            if (dateDiff > 0) {
                formattedMilliseconds = formatCallback(dateDiff);
                element.innerHTML = formattedMilliseconds;
                window.setTimeout(function() { timerCallback(); }, 1000);
            } else {
                timeoutCallback();
            }
        };

        timerCallback();
    };

    Conference._DefaultFormatCallback = function(milliseconds) {
        var totalSeconds = Math.floor(milliseconds / 1000);
        var seconds = totalSeconds % 60;
        var totalMinutes = Math.floor(totalSeconds / 60);
        var minutes = totalMinutes % 60;
        var totalHours = Math.floor(totalMinutes / 60);
        var hours = totalHours % 24;
        var days = Math.floor(totalHours / 24);

        var secondsPart = [((seconds >= 10) ? '' : '0'), seconds.toString()].join('');
        var minutesPart = [((minutes >= 10) ? '' : '0'), minutes.toString()].join('');
        var hoursPart = [((hours >= 10) ? '' : '0'), hours.toString()].join('');
        var daysPart = [((days >= 10) ? '' : '0'), days.toString()].join('');

        var result = '';

        if (days > 0) {
            result = [daysPart, hoursPart, minutesPart, secondsPart].join(':');
        } else if (hours > 0) {
            result = [hoursPart, minutesPart, secondsPart].join(':');
        } else {
            result = [minutesPart, secondsPart].join(':');
        }

        return result;
    };

    Conference._DefaultTimeoutCallback = function(element) {
        return function() {
            element.innerHTML = '';
        };
    };

    $('.reg-time-info__title').each(function() {
        var redirectUrl = this.getAttribute('data-redirectUrl');
        Conference.StartTimer(this, function() { window.location = redirectUrl; });
    });
});