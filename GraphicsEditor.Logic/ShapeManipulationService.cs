using GraphicsEditor.Core.Models;
using GraphicsEditor.Core.Geometry;

namespace GraphicsEditor.Logic;

/// <summary>
/// Service for handling shape manipulation operations like resizing and handle detection.
/// </summary>
public class ShapeManipulationService
{
    private const double DefaultHandleSize = 8.0;
    private const double DefaultTolerance = 2.0;

    /// <summary>
    /// Detects which resize handle (if any) is at the specified point for a given shape.
    /// </summary>
    public ResizeHandle GetHandleAtPoint(IShape shape, double x, double y, double handleSize = DefaultHandleSize)
    {
        var bounds = shape.GetBounds();
        double tolerance = handleSize / 2 + DefaultTolerance;
        
        // Check corner handles first
        if (Math.Abs(x - bounds.X) < tolerance && Math.Abs(y - bounds.Y) < tolerance)
            return ResizeHandle.TopLeft;
        if (Math.Abs(x - (bounds.X + bounds.Width)) < tolerance && Math.Abs(y - bounds.Y) < tolerance)
            return ResizeHandle.TopRight;
        if (Math.Abs(x - bounds.X) < tolerance && Math.Abs(y - (bounds.Y + bounds.Height)) < tolerance)
            return ResizeHandle.BottomLeft;
        if (Math.Abs(x - (bounds.X + bounds.Width)) < tolerance && Math.Abs(y - (bounds.Y + bounds.Height)) < tolerance)
            return ResizeHandle.BottomRight;
        
        // Check mid-point handles for rectangles and lines
        if (shape is Rectangle || shape is Line)
        {
            if (Math.Abs(x - (bounds.X + bounds.Width / 2)) < tolerance && Math.Abs(y - bounds.Y) < tolerance)
                return ResizeHandle.Top;
            if (Math.Abs(x - (bounds.X + bounds.Width / 2)) < tolerance && Math.Abs(y - (bounds.Y + bounds.Height)) < tolerance)
                return ResizeHandle.Bottom;
            if (Math.Abs(x - bounds.X) < tolerance && Math.Abs(y - (bounds.Y + bounds.Height / 2)) < tolerance)
                return ResizeHandle.Left;
            if (Math.Abs(x - (bounds.X + bounds.Width)) < tolerance && Math.Abs(y - (bounds.Y + bounds.Height / 2)) < tolerance)
                return ResizeHandle.Right;
        }
        
        return ResizeHandle.None;
    }

    /// <summary>
    /// Gets the positions of all resize handles for a given shape.
    /// </summary>
    public IEnumerable<(double X, double Y, ResizeHandle Handle)> GetHandlePositions(IShape shape)
    {
        var bounds = shape.GetBounds();
        var handles = new List<(double X, double Y, ResizeHandle Handle)>
        {
            (bounds.X, bounds.Y, ResizeHandle.TopLeft),
            (bounds.X + bounds.Width, bounds.Y, ResizeHandle.TopRight),
            (bounds.X, bounds.Y + bounds.Height, ResizeHandle.BottomLeft),
            (bounds.X + bounds.Width, bounds.Y + bounds.Height, ResizeHandle.BottomRight),
        };
        
        // Mid-point handles for rectangles and lines
        if (shape is Rectangle || shape is Line)
        {
            handles.Add((bounds.X + bounds.Width / 2, bounds.Y, ResizeHandle.Top));
            handles.Add((bounds.X + bounds.Width / 2, bounds.Y + bounds.Height, ResizeHandle.Bottom));
            handles.Add((bounds.X, bounds.Y + bounds.Height / 2, ResizeHandle.Left));
            handles.Add((bounds.X + bounds.Width, bounds.Y + bounds.Height / 2, ResizeHandle.Right));
        }
        
        return handles;
    }

    /// <summary>
    /// Resizes a shape based on the active handle and delta movement.
    /// </summary>
    public void ResizeShape(IShape shape, ResizeHandle handle, double deltaX, double deltaY, Point2D currentMousePosition)
    {
        if (shape is Rectangle rect)
        {
            ResizeRectangle(rect, handle, deltaX, deltaY);
        }
        else if (shape is Circle circle)
        {
            ResizeCircle(circle, handle, currentMousePosition);
        }
        else if (shape is Line line)
        {
            ResizeLine(line, handle, deltaX, deltaY);
        }
    }

    private void ResizeRectangle(Rectangle rect, ResizeHandle handle, double deltaX, double deltaY)
    {
        switch (handle)
        {
            case ResizeHandle.TopLeft:
                rect.X += deltaX;
                rect.Y += deltaY;
                rect.Width -= deltaX;
                rect.Height -= deltaY;
                break;
            case ResizeHandle.TopRight:
                rect.Y += deltaY;
                rect.Width += deltaX;
                rect.Height -= deltaY;
                break;
            case ResizeHandle.BottomLeft:
                rect.X += deltaX;
                rect.Width -= deltaX;
                rect.Height += deltaY;
                break;
            case ResizeHandle.BottomRight:
                rect.Width += deltaX;
                rect.Height += deltaY;
                break;
            case ResizeHandle.Top:
                rect.Y += deltaY;
                rect.Height -= deltaY;
                break;
            case ResizeHandle.Bottom:
                rect.Height += deltaY;
                break;
            case ResizeHandle.Left:
                rect.X += deltaX;
                rect.Width -= deltaX;
                break;
            case ResizeHandle.Right:
                rect.Width += deltaX;
                break;
        }
        
        // Ensure minimum size
        if (rect.Width < 10) rect.Width = 10;
        if (rect.Height < 10) rect.Height = 10;
    }

    private void ResizeCircle(Circle circle, ResizeHandle handle, Point2D mousePosition)
    {
        // For circles, only corner handles work - they change the radius
        var centerX = circle.Center.X;
        var centerY = circle.Center.Y;
        
        switch (handle)
        {
            case ResizeHandle.TopLeft:
            case ResizeHandle.TopRight:
            case ResizeHandle.BottomLeft:
            case ResizeHandle.BottomRight:
                // Calculate new radius based on distance from center to mouse position
                var newRadius = Math.Sqrt(
                    Math.Pow(mousePosition.X - centerX, 2) + 
                    Math.Pow(mousePosition.Y - centerY, 2)
                );
                circle.Radius = Math.Max(5, newRadius); // Minimum radius of 5
                break;
        }
    }

    private void ResizeLine(Line line, ResizeHandle handle, double deltaX, double deltaY)
    {
        switch (handle)
        {
            case ResizeHandle.TopLeft:
                line.StartPoint = new Point2D(line.StartPoint.X + deltaX, line.StartPoint.Y + deltaY);
                break;
            case ResizeHandle.BottomRight:
                line.EndPoint = new Point2D(line.EndPoint.X + deltaX, line.EndPoint.Y + deltaY);
                break;
            case ResizeHandle.TopRight:
            case ResizeHandle.BottomLeft:
                // For lines, top-right and bottom-left also move endpoints
                if (line.StartPoint.Y < line.EndPoint.Y)
                {
                    if (handle == ResizeHandle.TopRight)
                        line.StartPoint = new Point2D(line.StartPoint.X + deltaX, line.StartPoint.Y + deltaY);
                    else
                        line.EndPoint = new Point2D(line.EndPoint.X + deltaX, line.EndPoint.Y + deltaY);
                }
                else
                {
                    if (handle == ResizeHandle.TopRight)
                        line.EndPoint = new Point2D(line.EndPoint.X + deltaX, line.EndPoint.Y + deltaY);
                    else
                        line.StartPoint = new Point2D(line.StartPoint.X + deltaX, line.StartPoint.Y + deltaY);
                }
                break;
            case ResizeHandle.Top:
            case ResizeHandle.Bottom:
            case ResizeHandle.Left:
            case ResizeHandle.Right:
                // Mid-point handles move the closer endpoint
                var midX = (line.StartPoint.X + line.EndPoint.X) / 2;
                var midY = (line.StartPoint.Y + line.EndPoint.Y) / 2;
                
                if (handle == ResizeHandle.Top || handle == ResizeHandle.Bottom)
                {
                    if (Math.Abs(line.StartPoint.Y - midY) < Math.Abs(line.EndPoint.Y - midY))
                        line.StartPoint = new Point2D(line.StartPoint.X, line.StartPoint.Y + deltaY);
                    else
                        line.EndPoint = new Point2D(line.EndPoint.X, line.EndPoint.Y + deltaY);
                }
                else // Left or Right
                {
                    if (Math.Abs(line.StartPoint.X - midX) < Math.Abs(line.EndPoint.X - midX))
                        line.StartPoint = new Point2D(line.StartPoint.X + deltaX, line.StartPoint.Y);
                    else
                        line.EndPoint = new Point2D(line.EndPoint.X + deltaX, line.EndPoint.Y);
                }
                break;
        }
    }
}
