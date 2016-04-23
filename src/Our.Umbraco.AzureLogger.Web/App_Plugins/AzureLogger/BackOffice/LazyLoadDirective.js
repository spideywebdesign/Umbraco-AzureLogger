﻿angular
    .module('umbraco')
    .directive('lazyLoad', ['$timeout', function ($timeout) {

        return {
            restrict: 'A',
            link: function (scope, element, attrs) {

                // returns true if the element doesn't stretch below the bottom of the view
                var elementCanExpand = function () {
                    return (element.offset().top + element.height() < $(window).height() + 500); // 500 = number of pixels below view
                };

                // initialize to ensure content is loaded to fill the initial screen (before any scroll activity)
                var lazyLoad = function () {
                    $timeout(function () { //timeout to ensure scope is ready
                        // TODO: to make more generic, check to see if a promise is actually returned
                        scope.$apply(attrs.lazyLoad) // execute angular expression string
                        .then(function (canLoadMore) {
                            if (canLoadMore && elementCanExpand()) { // check to see if screen filled
                                lazyLoad(); // try again
                            }
                        });
                    });
                };

                lazyLoad(); // init

                // timer to delay event until scrolling stopped
                var delayTimer;

                // jQuery find div with class 'umb-scrollable', as it's this outer element that is scrolled
                $(element).closest('.umb-scrollable').bind('scroll', function () {

                    // cancel any previous timer
                    if (delayTimer) { $timeout.cancel(delayTimer); }

                    // set new timer
                    delayTimer = $timeout(function () {
                        if (elementCanExpand()) {
                            scope.$apply(attrs.lazyLoad);
                        }
                    }, 250);

                });
            }
        }
    }]);