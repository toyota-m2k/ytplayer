using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace ytplayer.common {
    public static class RectUtil {
        public static double CenterX(this Rect r) {
            return r.Left + r.Width / 2;
        }
        public static double CenterY(this Rect r) {
            return r.Top + r.Height / 2;
        }
        public static Point Center(this Rect r) {
            return new Point(r.CenterX(), r.CenterY());
        }

        public static Vector Minus(this Point p, Point p2) {
            return new Vector(p.X - p2.X, p.Y - p2.Y);
        }
        
        public static Point Plus(this Point p, double dx, double dy) {
            return new Point(p.X + dx, p.Y + dy);
        }
        
        public static Point Plus(this Point p, Vector v) {
            return p.Plus(v.X, v.Y);
        }


        public static Rect Move(this Rect r, double dx, double dy) {
            r.Location = r.Location.Plus(dx, dy);
            return r;
        }

        public static Rect Move(this Rect r, Vector v) {
            return r.Move(v.X, v.Y);
        }
        public static Rect MoveLTTo(this Rect r, Point p) {
            r.Location = p;
            return r;
        }
        public static Rect MoveRBTo(this Rect r, Point p) {
            return r.Move(p.Minus(r.BottomRight));
        }
        public static Rect MoveCenterTo(this Rect r, Point p) {
            return r.Move(p.Minus(r.Center()));
        }

        public static Size Zoom(this Size s, double zoom) {
            s.Width *= zoom;
            s.Height *= zoom;
            return s;
        }

        public static Rect Zoom(this Rect r, double zoom, Point pivot) {
            r.Size = r.Size.Zoom(zoom);
            var v = r.TopLeft.Minus(pivot);
            return r.MoveLTTo(pivot.Plus(v*zoom));
        }



    }
}
