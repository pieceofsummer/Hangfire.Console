﻿using System;
using System.Collections;
using System.Collections.Generic;

namespace Hangfire.Console.Progress
{
    /// <summary>
    /// Non-generic version of <see cref="IEnumerable"/> wrapper.
    /// </summary>
    internal class ProgressEnumerable : IEnumerable
    {
        private readonly IEnumerable _enumerable;
        private readonly IProgressBar _progressBar;
        private readonly int _count;
        private readonly int _percentageIncrementStep;

        public ProgressEnumerable(IEnumerable enumerable, IProgressBar progressBar, int count)
        {
            if (enumerable == null)
                throw new ArgumentNullException(nameof(enumerable));
            if (progressBar == null)
                throw new ArgumentNullException(nameof(progressBar));
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count));

            _enumerable = enumerable;
            _progressBar = progressBar;
            _count = count;
        }

        public ProgressEnumerable(IEnumerable enumerable, IProgressBar progressBar, int count, int percentageIncrementStep) : this(enumerable, progressBar, count)
        {
            if (percentageIncrementStep <= 0)
                throw new ArgumentOutOfRangeException(nameof(percentageIncrementStep));

            _percentageIncrementStep = percentageIncrementStep;
        }

        public IEnumerator GetEnumerator()
        {
            return new Enumerator(_enumerable.GetEnumerator(), _progressBar, _count, _percentageIncrementStep);
        }

        private class Enumerator : IEnumerator, IDisposable
        {
            private readonly IEnumerator _enumerator;
            private readonly IProgressBar _progressBar;
            private int _count, _index, _percentageIncrementStep, _segmentThreshold;

            public Enumerator(IEnumerator enumerator, IProgressBar progressBar, int count, int percentageIncrementStep)
            {
                _enumerator = enumerator;
                _progressBar = progressBar;
                _count = count;
                _index = -1;
                _percentageIncrementStep = percentageIncrementStep;
                CalculateSegmentThreshold();
            }

            public object Current => _enumerator.Current;

            public void Dispose()
            {
                try
                {
                    (_enumerator as IDisposable)?.Dispose();
                }
                finally
                {
                    _progressBar.SetValue(100);
                }
            }

            public bool MoveNext()
            {
                var r = _enumerator.MoveNext();
                if (r)
                {
                    _index++;

                    if (_index >= _count)
                    {
                        // adjust maxCount if overrunned
                        _count = _index + 1;
                        CalculateSegmentThreshold();
                    }

                    if (_percentageIncrementStep > 0)
                    {
                        // update only when current index has hit next progress threshold
                        int remainder = _index % _segmentThreshold;

                        if (remainder == 0)
                            _progressBar.SetValue((_index / _segmentThreshold) * _percentageIncrementStep);
                    }
                    else
                        _progressBar.SetValue(_index * 100.0 / _count);
                }
                return r;
            }

            public void Reset()
            {
                _enumerator.Reset();
                _index = -1;
            }

            private void CalculateSegmentThreshold()
            {
                _segmentThreshold = (int)((_percentageIncrementStep / 100.0) * _count);
            }
        }
    }

    /// <summary>
    /// Generic version of <see cref="IEnumerable{T}"/> wrapper.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal class ProgressEnumerable<T> : IEnumerable<T>
    {
        private readonly IEnumerable<T> _enumerable;
        private readonly IProgressBar _progressBar;
        private readonly int _count;
        private readonly int _percentageIncrementStep;

        public ProgressEnumerable(IEnumerable<T> enumerable, IProgressBar progressBar, int count)
        {
            if (enumerable == null)
                throw new ArgumentNullException(nameof(enumerable));
            if (progressBar == null)
                throw new ArgumentNullException(nameof(progressBar));
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count));

            _enumerable = enumerable;
            _progressBar = progressBar;
            _count = count;
        }

        public ProgressEnumerable(IEnumerable<T> enumerable, IProgressBar progressBar, int count, int percentageIncrementStep) : this(enumerable, progressBar, count)
        {
            if (percentageIncrementStep <= 0)
                throw new ArgumentOutOfRangeException(nameof(percentageIncrementStep));

            _percentageIncrementStep = percentageIncrementStep;
        }

        public IEnumerator<T> GetEnumerator()
        {
            return new Enumerator(_enumerable.GetEnumerator(), _progressBar, _count, _percentageIncrementStep);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return new Enumerator(_enumerable.GetEnumerator(), _progressBar, _count, _percentageIncrementStep);
        }

        private class Enumerator : IEnumerator<T>
        {
            private readonly IEnumerator<T> _enumerator;
            private readonly IProgressBar _progressBar;
            private int _count, _index, _percentageIncrementStep, _segmentThreshold;

            public Enumerator(IEnumerator<T> enumerator, IProgressBar progressBar, int count, int percentageIncrementStep)
            {
                _enumerator = enumerator;
                _progressBar = progressBar;
                _count = count;
                _index = -1;
                _percentageIncrementStep = percentageIncrementStep;
                CalculateSegmentThreshold();
            }

            public T Current => _enumerator.Current;

            object IEnumerator.Current => ((IEnumerator)_enumerator).Current;

            public void Dispose()
            {
                try
                {
                    _enumerator.Dispose();
                }
                finally
                {
                    _progressBar.SetValue(100);
                }
            }

            public bool MoveNext()
            {
                var r = _enumerator.MoveNext();
                if (r)
                {
                    _index++;

                    if (_index >= _count)
                    {
                        // adjust maxCount if overrunned
                        _count = _index + 1;
                        CalculateSegmentThreshold();
                    }

                    if (_percentageIncrementStep > 0)
                    {
                        // update only when current index has hit next progress threshold
                        int remainder = _index % _segmentThreshold;

                        if (remainder == 0)
                            _progressBar.SetValue((_index / _segmentThreshold) * _percentageIncrementStep);
                    }
                    else
                        _progressBar.SetValue(_index * 100.0 / _count);
                }
                return r;
            }

            public void Reset()
            {
                _enumerator.Reset();
                _index = -1;
            }

            private void CalculateSegmentThreshold()
            {
                _segmentThreshold = (int)((_percentageIncrementStep / 100.0) * _count);
            }
        }
    }
}
