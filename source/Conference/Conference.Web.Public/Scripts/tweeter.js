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

$(function () {
    function getTweets() {
        var user = $("#tweets").attr("data-user");
        var url = 'http://search.twitter.com/search.json?callback=?&q=@@' + user;
        $.getJSON(url, function (json) {
            var output = [];
            for (var i = 0, len = json.results.length; i < len; i++) {

                //instead of appending each result, add each to the buffer array
                //output.push('<p><img src="' + json.results[i].profile_image_url + '" widt="48" height="48" />' + json.results[i].text + '</p>');

                output.push('<span class="tile__tweet"><span class="tile__nick"><span class="tile__time">4h ago</span>@' + json.results[i].from_user + '</span>' + json.results[i].text + '</span>');
            }

            //now select the #results element only once and append all the output at once, then slide it into view
            $("#tweets").html(output.join('')).slideDown('slow');
        });
    }

    setInterval(getTweets, 20000);

    //run the getTweets function on document.ready
    getTweets();

});