/*
 *  Part of PPather
 *  Copyright Pontus Borg 2008
 *
 */

namespace WowTriangles
{
    // Fully automatic triangle loader for a MPQ/WoW world

    public abstract class TriangleSupplier
    {
        public abstract void GetTriangles(TriangleCollection to, float min_x, float min_y, float max_x, float max_y);

        public virtual void Close()
        {
        }
    }
}