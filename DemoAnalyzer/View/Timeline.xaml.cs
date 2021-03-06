﻿using DemoAnalyzer.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace DemoAnalyzer
{
    /// <summary>
    /// Interaction logic for Timeline.xaml
    /// </summary>
    public partial class Timeline : UserControl
    {
        private int _lastTick;
        private int _playbackPosition = 0;
        private int _selectionStart = 0;
        private int _selectionEnd = 0;

        private RoundInfo[] _rounds;
        private Line[] _lines;

        public int LastTick => _lastTick;
        public int PlaybackPosition
        {
            get => _playbackPosition;
            set
            {
                if (_lastTick == 0)
                    throw new InvalidOperationException();

                if (_playbackPosition != value)
                {
                    _playbackPosition = value;

                    var canvasPosition = TickToCanvasPosition(value);

                    playback.X1 = canvasPosition;
                    playback.X2 = canvasPosition;

                    PlaybackPositionChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }
        public int SelectionStart => selection.Visibility == Visibility.Visible ? _selectionStart : -1;
        public int SelectionEnd => selection.Visibility == Visibility.Visible ? _selectionEnd : -1;

        public event EventHandler PlaybackPositionChanged;

        public Timeline()
        {
            InitializeComponent();
        }

        public void Init(IReadOnlyList<RoundInfo> rounds, int lastTick)
        {
            _playbackPosition = 0;
            _lastTick = lastTick;

            playback.Visibility = Visibility.Visible;
            playback.X1 = 0;
            playback.X2 = 0;

            _rounds = rounds.ToArray();
            _lines = rounds.Select(x =>
            {
                var line = new Line();
                line.Stroke = Brushes.Gray;
                line.StrokeThickness = 2;
                canvas.Children.Insert(0, line);
                return line;
            }).ToArray();

            RepositionRounds(true, true);            

            PlaybackPositionChanged?.Invoke(this, EventArgs.Empty);
        }

        private void RepositionRounds(bool widthChanged, bool heightChanged)
        {
            if (_rounds == null)
                return;

            for (int i = 0; i < _rounds.Length; i++)
            {
                if (widthChanged)
                {
                    var position = TickToCanvasPosition(_rounds[i].StartTick);

                    _lines[i].X1 = position;
                    _lines[i].X2 = position;
                }

                if (heightChanged)
                {
                    _lines[i].Y1 = 0;
                    _lines[i].Y2 = canvas.ActualHeight;
                }
            }
        }

        public void Reset()
        {
            _playbackPosition = 0;
            _selectionStart = 0;
            _selectionEnd = 0;
            _lastTick = 0;

            hover.Visibility = Visibility.Hidden;
            playback.Visibility = Visibility.Hidden;
            selection.Visibility = Visibility.Hidden;

            if (_lines != null)
                canvas.Children.RemoveRange(0, _lines.Length);

            _rounds = null;
            _lines = null;
        }

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            base.OnRenderSizeChanged(sizeInfo);

            if (sizeInfo.WidthChanged && _lastTick != 0)
            {
                var canvasPosition = TickToCanvasPosition(_playbackPosition);

                playback.X1 = canvasPosition;
                playback.X2 = canvasPosition;

                if (selection.Visibility == Visibility.Visible)
                {
                    var start = Math.Min(_selectionStart, _selectionEnd);
                    var end = Math.Max(_selectionStart, _selectionEnd);

                    var startCanvasPosition = TickToCanvasPosition(start);
                    var endCanvasPosition = TickToCanvasPosition(end);

                    selection.Visibility = Visibility.Visible;
                    Canvas.SetLeft(selection, startCanvasPosition);
                    Canvas.SetTop(selection, 0);
                    selection.Width = endCanvasPosition - startCanvasPosition;
                    selection.Height = canvas.ActualHeight;
                }

                RepositionRounds(true, false);
            }

            if (sizeInfo.HeightChanged)
            {
                playback.Y1 = 0;
                playback.Y2 = canvas.ActualHeight;

                hover.Y1 = 0;
                hover.Y2 = canvas.ActualHeight;

                if (selection.Visibility == Visibility.Visible)
                {
                    Canvas.SetTop(selection, 0);
                    selection.Height = canvas.ActualHeight;
                }

                RepositionRounds(false, true);
            }
        }

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);

            if (_lastTick == 0)
                return;

            var tickByPosition = MousePositionToTick(e);
            var canvasPosition = TickToCanvasPosition(tickByPosition);

            if (_playbackPosition != tickByPosition)
            {
                _playbackPosition = tickByPosition;
                playback.X1 = canvasPosition;
                playback.X2 = canvasPosition;

                PlaybackPositionChanged?.Invoke(this, EventArgs.Empty);
            }

            _selectionStart = tickByPosition;
            selection.Visibility = Visibility.Hidden;

            e.MouseDevice.Capture(this);
        }

        private int GetRound(int tick)
        {
            for (int i = 0; i < _rounds.Length; i++)
                if (tick >= _rounds[i].StartTick && tick <= _rounds[i].EndTick)
                    return i;
            
            return -1;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            if (_lastTick == 0)
                return;

            var tickByPosition = MousePositionToTick(e);
            var canvasPosition = TickToCanvasPosition(tickByPosition);

            hoverInfo.Visibility = Visibility.Visible;

            var round = GetRound(tickByPosition);

            if (round == -1)
                hoverInfo.Text = $"Tick {tickByPosition}";
            else
                hoverInfo.Text = $"Tick {tickByPosition}, Round {round + 1}";

            Canvas.SetLeft(hoverInfo, canvasPosition);

            if (e.LeftButton == MouseButtonState.Pressed)
            {
                hover.Visibility = Visibility.Hidden;

                if (_playbackPosition != tickByPosition)
                {
                    _playbackPosition = tickByPosition;
                    playback.X1 = canvasPosition;
                    playback.X2 = canvasPosition;

                    PlaybackPositionChanged?.Invoke(this, EventArgs.Empty);
                }

                _selectionEnd = tickByPosition;

                var start = Math.Min(_selectionStart, _selectionEnd);
                var end = Math.Max(_selectionStart, _selectionEnd);

                if (end - start >= 500)
                {
                    var startCanvasPosition = TickToCanvasPosition(start);
                    var endCanvasPosition = TickToCanvasPosition(end);

                    selection.Visibility = Visibility.Visible;
                    Canvas.SetLeft(selection, startCanvasPosition);
                    Canvas.SetTop(selection, 0);
                    selection.Width = endCanvasPosition - startCanvasPosition;
                    selection.Height = canvas.ActualHeight;
                }
                else
                {
                    selection.Visibility = Visibility.Hidden;
                }
            }
            else
            {
                hover.Visibility = Visibility.Visible;
                hover.X1 = canvasPosition;
                hover.X2 = canvasPosition;
            }
        }

        protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonUp(e);

            e.MouseDevice.Capture(null);
        }

        protected override void OnMouseLeave(MouseEventArgs e)
        {
            base.OnMouseLeave(e);

            hover.Visibility = Visibility.Hidden;
            hoverInfo.Visibility = Visibility.Hidden;
        }

        private int MousePositionToTick(MouseEventArgs e)
        {
            var pos = e.MouseDevice.GetPosition(canvas);
            var percentage = (double)pos.X / canvas.ActualWidth;
            var ticks = percentage * _lastTick;

            return (int)Math.Round(ticks);
        }

        private double TickToCanvasPosition(int tick)
        {
            if (_lastTick == 0)
                throw new InvalidOperationException();

            var percentage = (double)tick / _lastTick;
            var canvasPosition = percentage * canvas.ActualWidth;

            return canvasPosition;
        }
    }
}
