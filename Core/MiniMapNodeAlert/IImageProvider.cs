using System;
using System.Drawing;

namespace Core
{
    public interface IImageProvider
    {
        event EventHandler<NodeEventArgs> NodeEvent;
    }

    public class NodeEventArgs : EventArgs
    {
        public Bitmap Bitmap { get; set; } = new Bitmap(1, 1);
        public Point Point { get; set; }
    }
}