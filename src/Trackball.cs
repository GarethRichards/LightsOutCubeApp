//------------------------------------------------------------------
//
//  The following article discusses the mechanics behind this
//  trackball implementation: http://viewport3d.com/trackball.htm
//
//  Reading the article is not required to use this sample code,
//  but skimming it might be useful.
//
//  For licensing information and to get the latest version go to:
//  http://workspaces.gotdotnet.com/3dtools
//
//  THIS CODE AND INFORMATION ARE PROVIDED "AS IS" WITHOUT WARRANTY
//  OF ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT
//  LIMITED TO THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR
//  FITNESS FOR A PARTICULAR PURPOSE.
//
//------------------------------------------------------------------

using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Media3D;
using System.Windows.Markup;

namespace _3DTools
{
    /// <summary>
    ///     Trackball is a utility class which observes the mouse events
    ///     on a specified FrameworkElement and produces a Transform3D
    ///     with the resultant rotation and scale.
    /// </summary>
    public class Trackball
    {
        private FrameworkElement _eventSource;
        private Point _previousPosition2D;
        private Vector3D _previousPosition3D = new Vector3D(0, 0, 1);

        private ScaleTransform3D _scale = new ScaleTransform3D();
        private AxisAngleRotation3D _rotation = new AxisAngleRotation3D();

        // tracking state to avoid using stale positions if capture is lost
        private bool _isTracking = false;

        public Trackball(Transform3DGroup Group)
        {
            Group.Children.Add(_scale);
            Group.Children.Add(new RotateTransform3D(_rotation));
        }

        /// <summary>
        ///     The FrameworkElement we listen to for mouse events.
        ///     Subscribes to extra events to keep state in sync (LostMouseCapture / MouseLeave).
        /// </summary>
        public FrameworkElement EventSource
        {
            get { return _eventSource; }

            set
            {
                if (_eventSource != null)
                {
                    _eventSource.MouseDown -= this.OnMouseDown;
                    _eventSource.MouseUp -= this.OnMouseUp;
                    _eventSource.MouseMove -= this.OnMouseMove;
                    _eventSource.LostMouseCapture -= this.OnLostMouseCapture;
                    _eventSource.MouseLeave -= this.OnMouseLeave;
                }

                _eventSource = value;

                if (_eventSource != null)
                {
                    _eventSource.MouseDown += this.OnMouseDown;
                    _eventSource.MouseUp += this.OnMouseUp;
                    _eventSource.MouseMove += this.OnMouseMove;
                    _eventSource.LostMouseCapture += this.OnLostMouseCapture;
                    _eventSource.MouseLeave += this.OnMouseLeave;
                }
            }
        }

        private void OnMouseDown(object sender, MouseEventArgs e)
        {
            // Start a tracking session
            Mouse.Capture(EventSource, CaptureMode.Element);
            _isTracking = true;

            _previousPosition2D = e.GetPosition(EventSource);
            _previousPosition3D = ProjectToTrackball(
                EventSource.ActualWidth,
                EventSource.ActualHeight,
                _previousPosition2D);
        }

        private void OnMouseUp(object sender, MouseEventArgs e)
        {
            // End tracking
            Mouse.Capture(EventSource, CaptureMode.None);
            _isTracking = false;
        }

        private void OnLostMouseCapture(object sender, MouseEventArgs e)
        {
            // Lost capture -> cancel tracking to avoid a big jump next time.
            // Also update previous positions to current mouse pos so resuming doesn't jump.
            try
            {
                if (_eventSource != null)
                {
                    var p = Mouse.GetPosition(_eventSource);
                    _previousPosition2D = p;
                    _previousPosition3D = ProjectToTrackball(_eventSource.ActualWidth, _eventSource.ActualHeight, p);
                }
            }
            catch
            {
                // ignore
            }

            _isTracking = false;
        }

        private void OnMouseLeave(object sender, MouseEventArgs e)
        {
            // If the user leaves the EventSource while not pressing, cancel tracking.
            if (Mouse.LeftButton != MouseButtonState.Pressed && Mouse.RightButton != MouseButtonState.Pressed)
            {
                _isTracking = false;
            }
            else
            {
                // If buttons are still pressed, refresh previous positions to avoid a jump when re-entering.
                try
                {
                    if (_eventSource != null)
                    {
                        var p = Mouse.GetPosition(_eventSource);
                        _previousPosition2D = p;
                        _previousPosition3D = ProjectToTrackball(_eventSource.ActualWidth, _eventSource.ActualHeight, p);
                    }
                }
                catch { }
            }
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            Point currentPosition = e.GetPosition(EventSource);

            // If left button is pressed but we are not tracking (capture was lost/ interrupted),
            // initialize the previous positions so we resume cleanly on the next movement.
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                if (!_isTracking)
                {
                    // start a fresh tracking baseline and skip applying a rotation for this frame
                    _previousPosition2D = currentPosition;
                    _previousPosition3D = ProjectToTrackball(EventSource.ActualWidth, EventSource.ActualHeight, currentPosition);
                    _isTracking = true;
                }
                else
                {
                    Track(currentPosition);
                }
            }
            else if (e.RightButton == MouseButtonState.Pressed)
            {
                // allow zoom even if _isTracking is false (right-button zoom)
                if (!_isTracking)
                {
                    _previousPosition2D = currentPosition;
                    _isTracking = true;
                }
                Zoom(currentPosition);
            }
            else
            {
                // no button pressed -> ensure tracking is off
                _isTracking = false;
            }

            _previousPosition2D = currentPosition;
        }

        private void Track(Point currentPosition)
        {
            Vector3D currentPosition3D = ProjectToTrackball(
                EventSource.ActualWidth, EventSource.ActualHeight, currentPosition);

            Vector3D axis = Vector3D.CrossProduct(_previousPosition3D, currentPosition3D);
            double angle = Vector3D.AngleBetween(_previousPosition3D, currentPosition3D);

            // Defensive guards: ignore tiny/invalid deltas and extremely large jumps
            if (double.IsNaN(angle) || angle == 0.0)
                return;

            // If axis is degenerate, ignore this frame
            if (axis.LengthSquared < 1e-8 || double.IsNaN(axis.X) || double.IsNaN(axis.Y) || double.IsNaN(axis.Z))
            {
                _previousPosition3D = currentPosition3D;
                return;
            }

            // Normalize axis before creating quaternion
            axis.Normalize();

            // Clamp angle to avoid giant jumps caused by stale previous position
            const double maxAngleDeg = 45.0;
            if (angle > maxAngleDeg)
                angle = maxAngleDeg;

            // Create the delta quaternion (note sign kept as before)
            Quaternion delta = new Quaternion(axis, -angle);

            // Compose *before* the current orientation (pre-multiply) so the delta
            // is applied in world coordinates rather than object-local coordinates.
            // This avoids the "weird" flips when the object already has a large rotation.
            Quaternion q = new Quaternion(_rotation.Axis, _rotation.Angle);
            q = delta * q;

            // guard against invalid quaternion results
            if (double.IsNaN(q.X) || double.IsNaN(q.Y) || double.IsNaN(q.Z) || double.IsNaN(q.W))
            {
                _previousPosition3D = currentPosition3D;
                return;
            }

            q.Normalize();

            var newAxis = q.Axis;
            var newAngle = q.Angle;
            if (!double.IsNaN(newAxis.X) && !double.IsNaN(newAxis.Y) && !double.IsNaN(newAxis.Z) && !double.IsNaN(newAngle))
            {
                _rotation.Axis = newAxis;
                _rotation.Angle = newAngle;
            }

            _previousPosition3D = currentPosition3D;
        }

        private Vector3D ProjectToTrackball(double width, double height, Point point)
        {
            // Use the smaller viewport dimension so the virtual trackball stays circular
            double minDim = Math.Min(width, height);
            if (minDim <= 0) return new Vector3D(0, 0, 1);

            // Map pixel to [-1,1] centered coordinates using minDim
            double x = (point.X - width * 0.5) / (minDim * 0.5);
            double y = (height * 0.5 - point.Y) / (minDim * 0.5); // +Y up

            // Flip horizontal direction to match expected drag feel (tweak/remove if you prefer inverted)
            x = -x;

            // Compute z on unit sphere (or clamp to disk)
            double r2 = x * x + y * y;
            double z = r2 <= 1.0 ? Math.Sqrt(1.0 - r2) : 0.0;

            return new Vector3D(x, y, z);
        }

        private void Zoom(Point currentPosition)
        {
            double yDelta = currentPosition.Y - _previousPosition2D.Y;

            double scale = Math.Exp(yDelta / 100);    // e^(yDelta/100) is fairly arbitrary.

            _scale.ScaleX *= scale;
            _scale.ScaleY *= scale;
            _scale.ScaleZ *= scale;
        }

        public void TrackX(double x)
        {
            Vector3D axis = new Vector3D(1, 0, 0);
            double angle = x;
            Quaternion delta = new Quaternion(axis, -angle);

            // pre-multiply so the rotation is in world space
            Quaternion q = new Quaternion(_rotation.Axis, _rotation.Angle);
            q = delta * q;
            q.Normalize();
            _rotation.Axis = q.Axis;
            _rotation.Angle = q.Angle;
        }

        public void TrackY(double y)
        {
            Vector3D axis = new Vector3D(0, 1, 0);
            double angle = y;
            Quaternion delta = new Quaternion(axis, -angle);

            Quaternion q = new Quaternion(_rotation.Axis, _rotation.Angle);
            q = delta * q;
            q.Normalize();
            _rotation.Axis = q.Axis;
            _rotation.Angle = q.Angle;
        }

        public void TrackZ(double y)
        {
            Vector3D axis = new Vector3D(0, 0, 1);
            double angle = y;
            Quaternion delta = new Quaternion(axis, -angle);

            Quaternion q = new Quaternion(_rotation.Axis, _rotation.Angle);
            q = delta * q;
            q.Normalize();
            _rotation.Axis = q.Axis;
            _rotation.Angle = q.Angle;
        }
    }
}
