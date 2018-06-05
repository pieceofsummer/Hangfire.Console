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
            this._progress = null;
            this._bar = null;
            this._value = null;
        }

        JobProgress.prototype._create = function() {
            var parent = $('td:last-child', this._row);
            this._progress = $('<div class="progress-circle">\n' +
                '  <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24">\n' +
                '    <circle r="11" cx="12" cy="12" />\n' +
                '    <circle class="bar" r="11" cx="12" cy="12" stroke-dasharray="69.12" stroke-dashoffset="69.12" />\n' +
                '  </svg>\n' +
                '</div>').prependTo(parent);
            this._bar = $('.bar', this._progress);
        };
        
        JobProgress.prototype._destroy = function() {
            if (this._progress)
                this._progress.remove();
            
            this._progress = null;
            this._bar = null;
            this._value = null;
        };

        JobProgress.prototype.update = function(value) {
            if (typeof value !== 'number' || value < 0) {
                this._destroy();
                return;
            }
            
            value = Math.min(Math.round(value), 100);

            if (!this._progress) {
                this._create();
            } else if (this._value === value) {
                return;
            }

            var r = this._bar.attr('r'),
                c = Math.PI * r * 2;
            
            this._bar.css('stroke-dashoffset', ((100 - value) / 100) * c);
            this._progress.attr('data-value', value);
            this._value = value;
        };
        
        return JobProgress;
    })();

    hangfire.JobProgressPoller = (function() {
        function JobProgressPoller() {
            var jobsProgress = {};
            $(".js-jobs-list-row").each(function () {
                var $this = $(this),
                    jobId = $("input[name='jobs[]']", $this).val();
                if (jobId)
                    jobsProgress[jobId] = new Hangfire.JobProgress($this);
            });

            this._jobsProgress = jobsProgress;
            this._jobIds = Object.getOwnPropertyNames(jobsProgress);
            this._timerId = null;
            this._timerCallback = null;
        }

        JobProgressPoller.prototype.start = function () {
            if (this._jobIds.length === 0) return;

            var self = this;
            this._timerCallback = function() {
                $.post(pollUrl + 'progress', { 'jobs[]': self._jobIds }, function(data) {
                    var jobsProgress = self._jobsProgress;
                    Object.getOwnPropertyNames(data).forEach(function (jobId) {
                        var progress = jobsProgress[jobId],
                            value = data[jobId];
                        if (progress)
                            progress.update(value);
                    });
                }).always(function () {
                    if (self._timerCallback) {
                        self._timerId = setTimeout(self._timerCallback, pollInterval);
                    } else {
                        self._timerId = null;
                    }
                });
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
        
    } else if (/\/jobs\/processing$/.test(path)) {
        // execute scripts for /jobs/processing
        
        Hangfire.page._jobProgressPoller = new Hangfire.JobProgressPoller();
        Hangfire.page._jobProgressPoller.start();
    }
});