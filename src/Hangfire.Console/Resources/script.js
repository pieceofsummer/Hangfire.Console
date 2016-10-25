(function (hangfire) {
    hangfire.Console = (function () {
        function Console (el) {
            this._el = el;
            this._id = el.data('id');
            this._n = parseInt(el.data('n')) || 0;
        }

        Console.prototype._load = function (start, replace) {
            if (start < 0) return true;

            var url = hangfire.config && hangfire.config.pollUrl;
            if (!url) return false;

            url = url.replace(/\/stats$/, "/console/" + this._id);

            var $this = this;
            return $.get(url, { start: start }, function (data) {
                var $data = $(data);
                $this._n = parseInt($data.data('n'));

                // add lines
                if (replace) $this._el.empty();
                $this._el.append($(".line", $data));

                // set tooltips on new lines
                $(".line span[data-moment-title]:not([title])", $this._el).each(function () {
                    var $this = $(this),
                        time = moment($this.data('moment-title'), 'X');
                    $this.prop('title', time.format('llll'))
                         .attr('data-container', 'body');
                }).tooltip();
            }, "html");
        }

        Console.prototype.reload = function () {
            this._load(0, true);
        }

        Console.prototype.poll = function () {
            if (this._timerId) return;

            if (this._n < 0) {
                this._el.removeClass('active');
                return;
            }

            var interval = 1000;

            var $this = this;

            this._el.addClass('active');
            this._timerId = setInterval(function () {
                if (!$this._load($this._n, false) || $this._n < 0) {
                    $this._el.removeClass('active');
                    clearInterval($this._timerId);
                    $this._timerId = null;

                    if ($this._n === -1) {
                        // job has changed its state (but still exists)
                        location.reload();
                    }
                }
            }, interval);
        }

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