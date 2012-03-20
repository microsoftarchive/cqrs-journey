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

var Conference = {};

Conference.StartTimer = function(targetDate, elementId, timeoutCallback, formatCallback) {
    timeoutCallback = (typeof timeoutCallback === 'undefined') ? Conference._DefaultTimeoutCallback(elementId) : timeoutCallback;
    formatCallback = (typeof formatCallback === 'undefined') ? Conference._DefaultFormatCallback : formatCallback;

    var timerCallback = function () {
        var formattedMilliseconds = '';
        var currentDate = new Date();

        var dateDiff = targetDate.getTime() - currentDate.getTime();
        if (dateDiff > 0) {
            formattedMilliseconds = formatCallback(dateDiff);
            document.getElementById(elementId).innerHTML = formattedMilliseconds;
            window.setTimeout(function () { timerCallback(); }, 1000);
        }
        else {
            timeoutCallback();
        }
    };

    timerCallback();
}

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
    }
    else if (hours > 0) {
        result = [hoursPart, minutesPart, secondsPart].join(':');
    }
    else {
        result = [minutesPart, secondsPart].join(':');
    }

    return result;
}

Conference._DefaultTimeoutCallback = function(elementId) {
    return function () {
        document.getElementById(elementId).innerHTML = '';
    };
}