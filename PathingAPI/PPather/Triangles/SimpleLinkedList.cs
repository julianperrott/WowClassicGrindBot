/*
 *  Part of PPather
 *  Copyright Pontus Borg 2008
 *
 */

using PatherPath;

namespace WowTriangles
{
    // Very primitive Doule linked list of integers
    // very fast to move nodes from one list to another
    public class SimpleLinkedList
    {
        public Node first;
        public Node last;
        private int nodes;

        public class Node
        {
            public Node next;
            public Node prev;
            public int val;

            public Node(int val)
            {
                this.val = val;
            }

            public bool IsLast()
            {
                return next == null;
            }
        }

        public Node GetFirst()
        {
            return first;
        }

        private Logger logger;

        public SimpleLinkedList(Logger logger)
        {
            this.logger = logger;
        }

        public void AddNew(int i)
        {
            Node n = new Node(i);
            n.next = first;
            n.prev = null;
            if (first != null)
                first.prev = n;
            first = n;
            if (last == null)
                last = n;

            nodes++;
        }

        private void Error(string error)
        {
            logger.WriteLine(error);
        }

        public void Check()
        {
            if (first != null && first.prev != null)
                Error("First element must have prev == null");
            if (last != null && last.next != null)
                Error("Last element must have next == null");
            if (Count != RealCount)
                Error("Count != RealCount");
        }

        public void Steal(Node n, SimpleLinkedList from)
        {
            // unlink n from other list

            if (n == from.first)
            { // n was first
                from.first = n.next;
                if (from.first != null)
                    from.first.prev = null;
            }
            else
            {
                n.prev.next = n.next;
            }

            if (n == from.last)
            { // n was last
                from.last = n.prev;
                if (from.last != null)
                    from.last.next = null;
            }
            else
            {
                n.next.prev = n.prev;
            }

            from.nodes--;

            if (first != null)
                first.prev = n;
            n.next = first;
            n.prev = null;
            first = n;
            if (last == null)
                last = n;

            nodes++;
        }

        public void StealAll(SimpleLinkedList other)
        {
            // put them first
            //Check();
            // other.Check();
            if (other.first == null)
                return; // other empty
            if (first == null) // me empty
            {
                first = other.first;
                last = other.last;
            }
            else
            {
                other.last.next = first;
                first.prev = other.last;
                first = other.first;

                other.last = null;
                other.first = null;
            }

            nodes += other.nodes;
            other.nodes = 0;
            //Check();
            //other.Check();
        }

        public int Count
        {
            get
            {
                return nodes;
            }
        }

        public int RealCount
        {
            get
            {
                int i = 0;
                Node rover = first;
                while (rover != null)
                {
                    i++;
                    rover = rover.next;
                }
                return i;
            }
        }
    }
}