(function ($, hangfire) {
    var pollUrl = hangfire.config.consolePollUrl;
    var pollInterval = hangfire.config.consolePollInterval;
    if (!pollUrl || !pollInterval)
        throw new Error("Hangfire.Console was not properly configured");

    hangfire.LineBuffer = (function () {
        function updateMoments(container) {
            $(".line span[data-moment-title]:not([title])", container).each(function () {
                var $this = $(this),
                    time = moment($this.data('moment-title'), 'X');
                $this.prop('title', time.format('llll'))
                     .attr('data-container', 'body');
            });
        }

        function LineBuffer(el) {
            if (!el || el.length !== 1)
                throw new Error("LineBuffer expects jQuery object with a single value");

            this._el = el;
            this._n = parseInt(el.data('n')) || 0;
            updateMoments(el);
        }

        LineBuffer.prototype.replaceWith = function (other) {
            if (!(other instanceof LineBuffer))
                throw new Error("LineBuffer.replaceWith() expects LineBuffer argument");

            this._el.replaceWith(other._el);

            this._n = other._n;
            this._el = other._el;

            $(".line span[data-moment-title]", this._el).tooltip();
        };

        LineBuffer.prototype.append = function (other) {
            if (!other) return;

            if (!(other instanceof LineBuffer))
                throw new Error("LineBuffer.append() expects LineBuffer argument");

            var el = this._el;

            $(".line.pb", other._el).each(function () {
                var $this = $(this),
                    $id = $this.data('id');

                var pv = $(".line.pb[data-id='" + $id + "'] .pv", el);
                if (pv.length === 0) return;

                var $pv = $(".pv", $this);

                pv.attr("style", $pv.attr("style"))
                  .attr("data-value", $pv.attr("data-value"));
                $this.addClass("ignore");
            });

            $(".line:not(.ignore)", other._el).addClass("new").appendTo(el);

            this._n = other._n;

            $(".line span[data-moment-title]", el).tooltip();
        };

        LineBuffer.prototype.next = function () {
            return this._n;
        };

        LineBuffer.prototype.unmarkNew = function () {
            $(".line.new", this._el).removeClass("new");
        };

        LineBuffer.prototype.getHTMLElement = function () {
            return this._el[0];
        };

        return LineBuffer;
    })();

    hangfire.Console = (function () {
        function Console(el) {
            if (!el || el.length !== 1)
                throw new Error("Console expects jQuery object with a single value");

            this._el = el;
            this._id = el.data('id');
            this._buffer = new hangfire.LineBuffer($(".line-buffer", el));
            this._polling = false;
        }

        Console.prototype.reload = function () {
            var self = this;

            $.get(pollUrl + this._id, null, function (data) {
                self._buffer.replaceWith(new hangfire.LineBuffer($(data)));
            }, "html");
        };

        function resizeHandler(e) {
            var obj = e.target || e.srcElement,
                $buffer = $(obj).closest(".line-buffer"),
                $console = $buffer.closest(".console");

            if (0 === $(".line:first", $buffer).length) {
                // collapse console area if there's no lines
                $console.height(0);
                return;
            }

            $console.height($buffer.outerHeight(false));
        }

        Console.prototype.poll = function () {
            if (this._polling) return;

            if (this._buffer.next() < 0) return;

            var self = this;

            this._polling = true;
            this._el.addClass('active');

            resizeHandler( { target: this._buffer.getHTMLElement() } );
            window.addResizeListener(this._buffer.getHTMLElement(), resizeHandler);

            console.log("polling was started");

            setTimeout(function () { self._poll(); }, pollInterval);
        };

        Console.prototype._poll = function () {
            this._buffer.unmarkNew();

            var next = this._buffer.next();
            if (next < 0) {
                this._endPoll();

                if (next === -1) {
                    console.log("job state change detected");
                    location.reload();
                }

                return;
            }

            var self = this;

            $.get(pollUrl + this._id, { start: next }, function (data) {
                var $data = $(data),
                    buffer = new hangfire.LineBuffer($data),
                    newLines = $(".line:not(.pb)", $data);
                self._buffer.append(buffer);
                self._el.toggleClass("waiting", newLines.length === 0);
            }, "html")

            .always(function () {
                setTimeout(function () { self._poll(); }, pollInterval);
            });
        };

        Console.prototype._endPoll = function () {
            console.log("polling was terminated");

            window.removeResizeListener(this._buffer.getHTMLElement(), resizeHandler);

            this._el.removeClass("active waiting");
            this._polling = false;
        };

        return Console;
    })();

    hangfire.JobProgress = (function () { 
        function JobProgress(row) {
            if (!row || row.length !== 1)
                throw new Error("JobProgress expects jQuery object with a single value");
            
            this._row = row;
            this._pb = $('.progress-circle', row);
            this._pv = $('.value-bar', this._pb);
            this._pt = $('span', this._pb);
            this._value = null;
        }
        
        JobProgress.prototype._create = function() {
            var cell = $('td:last-child', this._row);
            return $('<div class="progress-circle"><span></span><div class="left-half-clipper">' +
                '<div class="first50-bar"></div><div class="value-bar"></div></div></div>').prependTo(cell);
        };
        
        JobProgress.prototype.update = function(value) {
            if (typeof value !== 'number' || value < 0) {
                // invalid state
                this._pb.remove();
                this._pb = null;
                this._pv = null;
                this._pt = null;
                this._value = null;
                
            } else {
                value = Math.max(Math.round(value), 100); 

                if (!this._pb || this._pb.length === 0) {
                    this._pb = this._create();
                    this._pv = $('.value-bar', this._pb);
                    this._pt = $('span', this._pb);
                } else if (this._value === value) {
                    // previous value has not changed
                    return;
                }

                this._pb.toggleClass('over50', value >= 50);
                this._pv.css('transform', 'rotate(' + Math.round(value * 3.6, 1) + 'deg)');
                this._pt.text(value);
                this._value = value;
            }
        };

        return JobProgress;
    })();

    hangfire.JobProgressPoller = (function() {
        function JobProgressPoller() {
            var jobsProgress = {};
            $(".js-jobs-list-row").each(function () {
                var $this = $(this), jobId = $("input[name='jobs[]']", $this).val();
                if (jobId) jobsProgress[jobId] = new Hangfire.JobProgress($this);
            });

            this._jobsProgress = jobsProgress;
            this._jobIds = Object.getOwnPropertyNames(jobsProgress);
            this._timerId = null;
            this._timerCallback = null;
        }

        JobProgressPoller.prototype._update = function(data) {
            if (typeof data !== "object") return;
            
            for (var jobId in Object.getOwnPropertyNames(data)) {
                var progress = this._jobsProgress[jobId],
                    value = data[jobId];
                if (progress)
                    progress.update(value);
            }
        };
        
        JobProgressPoller.prototype._nextPoll = function() {
            if (this._timerCallback) {
                this._timerId = setTimeout(this._timerCallback, pollInterval);
            } else {
                this._timerId = null;
            }
        };
        
        JobProgressPoller.prototype.start = function () {
            if (this._jobIds.length === 0) return;

            var self = this;
            this._timerCallback = function() {
                $.post(pollUrl + 'progress', { 'jobs[]': self._jobIds }, function(data) { self._update(data) })
                 .always(function () { self._nextPoll() });
            };
            
            this._timerId = setTimeout(this._timerCallback, 50);
        };

        JobProgressPoller.prototype.stop = function () {
            this._timerCallback = null;
            if (this._timerId !== null) {
                clearTimeout(this._timerId);
                this._timerId = null;
            }
        };

        return JobProgressPoller;
    })();
    
})(jQuery, window.Hangfire = window.Hangfire || {});

$(function () {
    var path = window.location.pathname;
    
    if (/\/jobs\/details\/([^/]+)$/.test(path)) {
        // execute scripts for /jobs/details/<jobId>
        
        $(".console").each(function (index) {
            var $this = $(this),
                c = new Hangfire.Console($this);

            $this.data('console', c);

            if (index === 0) {
                // poll on the first console
                c.poll();
            } else if ($this.find(".line").length > 0) {
                // collapse outdated consoles
                $this.addClass("collapsed");
            }
        });

        $(".container").on("click", ".console.collapsed", function () {
            $(this).removeClass("collapsed");
        });
        
    } else if (path.endsWith("/jobs/processing")) {
        // execute scripts for /jobs/processing
        
        Hangfire.page._jobProgressPoller = new Hangfire.JobProgressPoller();
        Hangfire.page._jobProgressPoller.start();
    }
});