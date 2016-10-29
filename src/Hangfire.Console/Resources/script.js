(function (hangfire) {

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

            $(".line", other._el).addClass("new").appendTo(this._el);

            this._n = other._n;

            $(".line span[data-moment-title]", this._el).tooltip();
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

        var pollUrl = hangfire.config.consolePollUrl;
        var pollInterval = hangfire.config.consolePollInterval;
        if (!pollUrl || !pollInterval)
            throw new Error("Hangfire.Console was not properly configured");

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
        }

        function resizeHandler(e) {
            var obj = e.target || e.srcElement,
                $buffer = $(obj).closest(".line-buffer"),
                $console = $buffer.closest(".console");

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
        }

        Console.prototype._poll = function () {
            this._buffer.unmarkNew();

            var next = this._buffer.next();
            if (next < 0) {
                this._endPoll();

                if (next == -1) {
                    console.log("job state change detected");
                    location.reload();
                }

                return;
            }

            var self = this;

            $.get(pollUrl + this._id, { start: next }, function (data) {
                self._buffer.append(new hangfire.LineBuffer($(data)));
            }, "html")

            .always(function () {
                setTimeout(function () { self._poll(); }, pollInterval);
            });
        }

        Console.prototype._endPoll = function () {
            console.log("polling was terminated");

            window.removeResizeListener(this._buffer.getHTMLElement(), resizeHandler);

            this._el.removeClass('active');
            this._polling = false;
        };

        return Console;
    })();

})(window.Hangfire = window.Hangfire || {});

$(function () {
    $(".console").each(function (index) {
        var $this = $(this),
            c = new Hangfire.Console($this);

        $this.data('console', c);

        if (index === 0) {
            // poll on the first console
            c.poll();
        }
    });
});