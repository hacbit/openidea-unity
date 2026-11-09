using System;
using System.IO;
using System.Linq;

namespace OpenIDEA.Editor
{
    // Unity events only request work. Collection and writing happen once the Editor is idle.
    internal sealed class ProjectSynchronization
    {
        private const double DelaySeconds = 0.5;
        private readonly Func<bool> _isSelected;
        private readonly Func<bool> _isBusy;
        private readonly Func<double> _time;
        private readonly Func<string[]> _generate;
        private readonly Action<Exception> _reportError;
        private string[] _generatedFiles = Array.Empty<string>();
        private bool _pending;
        private bool _running;
        private bool _current;
        private double _nextRun;

        internal ProjectSynchronization(Func<bool> isSelected, Func<bool> isBusy, Func<double> time,
            Func<string[]> generate, Action<Exception> reportError)
        {
            _isSelected = isSelected;
            _isBusy = isBusy;
            _time = time;
            _generate = generate;
            _reportError = reportError;
        }

        internal void Request()
        {
            if (!_isSelected()) return;
            _current = false;
            _pending = true;
            _nextRun = _time() + DelaySeconds;
        }

        internal void Update() => TryRun(false);

        internal void SyncNow()
        {
            Request();
            TryRun(true);
        }

        internal void EnsureGenerated()
        {
            if (!_isSelected()) return;
            if (!_current || _generatedFiles.Any(path => !File.Exists(path))) Request();
            TryRun(true);
        }

        private void TryRun(bool immediate)
        {
            if (!_pending || _running) return;
            if (!_isSelected())
            {
                _pending = false;
                return;
            }
            if (_isBusy())
            {
                _nextRun = _time() + DelaySeconds;
                return;
            }
            if (!immediate && _time() < _nextRun) return;

            _pending = false;
            _running = true;
            try
            {
                _generatedFiles = _generate();
                // A request raised during generation must still get its own pass.
                _current = !_pending;
            }
            catch (Exception exception)
            {
                _current = false;
                _reportError(exception);
                // Retry on another change, explicit regeneration, or open, never every frame.
            }
            finally
            {
                _running = false;
            }
        }
    }
}
