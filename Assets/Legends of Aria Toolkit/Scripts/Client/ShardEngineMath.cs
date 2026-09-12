using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CoreUtil.ShardEngineMath
{    
    public struct Vec2
    {
        static public Vec2 Zero { get { return new Vec2(0.0, 0.0); } }

        static public bool GetLineIntersection(Vec2 a1, Vec2 a2, Vec2 b1, Vec2 b2, out Vec2 intersect)
        {
            float s1_x, s1_y, s2_x, s2_y;
            s1_x = a2.X - a1.X; s1_y = a2.Z - a1.Z;
            s2_x = b2.X - b1.X; s2_y = b2.Z - b1.Z;

            float s, t;
            s = (-s1_y * (a1.X - b1.X) + s1_x * (a1.Z - b1.Z)) / (-s2_x * s1_y + s1_x * s2_y);
            t = (s2_x * (a1.Z - b1.Z) - s2_y * (a1.X - b1.X)) / (-s2_x * s1_y + s1_x * s2_y);

            if (s >= 0 && s <= 1 && t >= 0 && t <= 1)
            {
                intersect = new Vec2(a1.X + (t * s1_x), a1.Z + (t * s1_y));
                return true;
            }

            intersect = Vec2.Zero;
            return false; // No collision
        }

        public float X
        {
            get { return x; }
            set { x = value; }
        }
        
        public float Z
        {
            get { return z; }
            set { z = value; }
        }

        public Vec2(Vec2 other) : this()
        {
            X = other.X;
            Z = other.Z;
        }

        public Vec2(Vec3 other) : this()
        {
            X = other.X;
            Z = other.Z;
        }

        public Vec2(float x, float y, float z) : this()
        {
            X = x;
            Z = z;
        }

        public Vec2(double x, double z) : this()
        {
            X = (float)x;
            Z = (float)z;
        }

        public float Distance(Vec2 other)
        {
            return ((float)(Math.Sqrt((Math.Pow(other.X - X, 2) + Math.Pow(other.Z - Z, 2))))).LimitPrecision();
        }

        public float Distance(Vec3 other)
        {
            return (float)(Math.Sqrt((Math.Pow(other.X - X, 2) + Math.Pow(other.Z - Z, 2))));
        }
        
        public float DistanceSquared(Vec2 other)
        {
            return ((float)((Math.Pow(other.X - X, 2) + Math.Pow(other.Z - Z, 2)))).LimitPrecision();
        }

        public Vec2 LerpTo(Vec2 end, float percent)
        {
            return (this + percent * (end - this));
        }

        public Vec2 Project(float _degrees, float _distance)
        {
            double radians = _degrees.ToRadians();
            return new Vec2(
                X + _distance * Math.Sin(radians),
                Z + _distance * Math.Cos(radians));
        }

        public float YAngleTo(Vec3 _other)
        {
            return (float)Math.Atan2(_other.X - this.X, _other.Z - this.Z).ToDegrees();
        }

        public static bool operator ==(Vec2 v1, Vec2 v2)
        {
            return v1.Equals(v2);
        }

        public static bool operator !=(Vec2 v1, Vec2 v2)
        {
            return !v1.Equals(v2);
        }

        public static Vec2 operator -(Vec2 v1, Vec2 v2)
        {
            return new Vec2(v1.X - v2.X, v1.Z - v2.Z);
        }

        public static Vec2 operator +(Vec2 v1, Vec2 v2)
        {
            return new Vec2(v1.X + v2.X, v1.Z + v2.Z);
        }

        public static Vec2 operator *(float scalar, Vec2 v1)
        {
            return new Vec2(v1.X * scalar, v1.Z * scalar);
        }

        public float DotProduct(Vec2 _other)
        {
            return X * _other.X + Z * _other.Z;
        }

        public override bool Equals(object obj)
        {
            var other = (Vec2)obj;

            return X.LimitPrecision() == other.X.LimitPrecision()
                && Z.LimitPrecision() == other.Z.LimitPrecision();
        }

        public override int GetHashCode()
        {
            return X.GetHashCode() ^
                Z.GetHashCode();
        }

        public float YAngleTo(Vec2 _other)
        {
            return (float)Math.Atan2(_other.X - this.X, _other.Z - this.Z).ToDegrees();
        }

        public override string ToString()
        {
            return "(" + X + "," + Z + ")";
        }

        private float x;
        private float z;
    }

    public struct Vec3
    {
        public static readonly Vec3 Zero = new Vec3(0.0f, 0.0f, 0.0f);

        public static Vec3 ConvertFrom(string _strVec)
        {
            // first remove any parenthesis and then split on commas
            string[] compStrs = _strVec.Trim(new char[] { '(', ')' }).Split(new char[] { ',' });
            if (compStrs.Length == 3)
            {
                float[] compF = compStrs.Select(comp => (float)Convert.ToDouble(comp)).ToArray();
                return new Vec3(compF[0], compF[1], compF[2]);
            }

            throw new Exception("[Vec3::ConvertFrom] String is invalid.");
        }


        public float X
        {
            get { return x; }
            set { x = value; }
        }

        public float Y
        {
            get { return y; }
            set { y = value; }
        }

        public float Z
        {
            get { return z; }
            set { z = value; }
        }

        public Vec3(Vec3 other) : this()
        {
            x = other.X;
            y = other.Y;
            z = other.Z;
        }

        public Vec3(Vec2 other) : this()
        {
            x = other.X;
            z = other.Z;
        }

        public Vec3(float _x, float _y, float _z) : this()
        {
            x = _x;
            y = _y;
            z = _z;
        }

        public Vec3(double _x, double _y, double _z) : this()
        {
            x = (float)_x;
            y = (float)_y;
            z = (float)_z;
        }        

        public float DistanceSquared(Vec3 other)
        {
            return (float)(Math.Pow(other.X - X, 2) + Math.Pow(other.Y - Y, 2) + Math.Pow(other.Z - Z, 2));
        }

        public float Distance(Vec3 other)
        {
            return (float)(Math.Sqrt((Math.Pow(other.X - X, 2) + Math.Pow(other.Y - Y, 2) + Math.Pow(other.Z - Z, 2))));
        }

        public float Distance2(Vec3 other)
        {
                return (float)(Math.Sqrt((Math.Pow(other.X - X, 2) + Math.Pow(other.Z - Z, 2))));
        }

        public Vec3 LerpTo(Vec3 end, float percent)
        {
            return (this + percent * (end - this));
        }

        public Vec3 Project(float _degrees, float _distance)
        {
            double radians = _degrees.ToRadians();
            return new Vec3(
                x + _distance * Math.Sin(radians),
                y,
                z + _distance * Math.Cos(radians));
        }

        public float YAngleTo(Vec3 _other)
        {
            return (float)Math.Atan2(_other.X - this.x, _other.Z - this.z).ToDegrees();
        }

        public Vec3 ProjectTowards(Vec3 _other,float distance)
        {
            return this.Project(this.YAngleTo(_other), distance);
        }

        public static Vec3 operator -(Vec3 v1, Vec3 v2)
        {
            return new Vec3(v1.X - v2.X, v1.Y - v2.Y, v1.Z - v2.Z);
        }

        public static Vec3 operator +(Vec3 v1, Vec3 v2)
        {
            return new Vec3(v1.X + v2.X, v1.Y + v2.Y, v1.Z + v2.Z);
        }

        public Vec3 Add(Vec3 _other) { return this + _other; }

        public static Vec3 operator *(float scalar, Vec3 v1)
        {
            return new Vec3(v1.X * scalar, v1.Y * scalar, v1.Z * scalar);
        }

        public static bool operator ==(Vec3 v1, Vec3 v2)
        {
            return v1.Equals(v2);
        }

        public static bool operator !=(Vec3 v1, Vec3 v2)
        {
            return !v1.Equals(v2);
        }

        public override bool Equals(object obj)
        {
            if (!(obj is Vec3))
                return false;

            Vec3 other = (Vec3)obj;
            return other.X == x && other.Y == y && other.Z == z;
        }

        public override int GetHashCode()
        {
            return x.GetHashCode() ^ y.GetHashCode() ^ z.GetHashCode();
        }

        public override string ToString()
        {
            return "(" + x + "," + y + "," + z + ")";
        }        
        
        private float x;
        private float y;
        private float z;
    }

    public class Rect2
    {
        public static Rect2 UnitCube = new Rect2(AARect2.UnitCube);

        // Because of how we use float.LimitPrecision to avoid floating-point
        // equality comparison failures due to floating-point rounding errors,
        // these must be truly read-only!

        public Vec2[] Points
        {
            get
            {
                var result = new Vec2[4];
                for (int i = 0; i < 4; i++)
                    result[i] = points[i];
                return result;
            }
        }
        public Vec2 TopLeft { get { return points[0]; } }
        public Vec2 TopRight { get { return points[1]; } }
        public Vec2 BottomLeft { get { return points[3]; } }
        public Vec2 BottomRight { get { return points[2]; } }
        public Vec2 Center
        {
            get
            {
                return new Vec2(
                    TopLeft.X + (BottomRight.X - TopLeft.X) / 2f,
                    TopLeft.Z + (BottomRight.Z - TopLeft.Z) / 2f
                    );
            }
        }

        public float Area
        {
            get
            {
                return TopLeft.Distance(TopRight) * TopLeft.Distance(BottomLeft);
            }
        }

        public Rect2() { points = new Vec2[4] { new Vec2(), new Vec2(), new Vec2(), new Vec2(), }; }

        public Rect2(Vec2[] _points) : this()
        {
            if (_points.Length != 4)
            {
                throw new Exception("[Rect2::Rect2] Invalid initializer array");
            }

            for (int i = 0; i < 4; i++)
                points[i] = _points[i];
        }

        public Rect2(Rect2 other)
        {
            for (int i = 0; i < 4; i++)
            {
                points[i] = other.Points[i];
            }
        }

        public Rect2(Vec2 _topLeft, Vec2 _topRight, Vec2 _bottomLeft, Vec2 _bottomRight)
        {
            points = new Vec2[4] { _topLeft, _topRight, _bottomLeft, _bottomRight, };
        }

        public Rect2(AARect2 other)
        {
            points = other.Points.ToArray();
        }

        public Rect2(Rect3 other)
        {
            for (int i = 0; i < 4; i++)
            {
                points[i] = new Vec2(other.Points[i].X, other.Points[i].Z);
            }
        }

        public Rect2 Add(Rect2 r, Vec3 v)
        {
            return new Rect2(new Vec2(r.TopLeft.X + v.X, r.TopLeft.Z + v.Z),
                             new Vec2(r.TopRight.X + v.X, r.TopRight.Z + v.Z),
                             new Vec2(r.BottomLeft.X + v.X, r.BottomLeft.Z + v.Z),
                             new Vec2(r.BottomRight.X + v.X, r.BottomRight.Z + v.Z));
        }

        public bool Contains(Vec3 _point)
        {
            return Contains(new Vec2(_point.X, _point.Z));
        }

        public bool Contains(Vec2 _point)
        {
            bool oddNodes = false;
            Int32 i, j = 3;

            for (i = 0; i < 4; i++)
            {
                if (points[i].Z < _point.Z && points[j].Z >= _point.Z
                    || points[j].Z < _point.Z && points[i].Z >= _point.Z)
                {
                    if (points[i].X + (_point.Z - points[i].Z) / (points[j].Z - points[i].Z) * (points[j].X - points[i].X) < _point.X)
                    {
                        oddNodes = !oddNodes;
                    }
                }

                j = i;
            }

            return oddNodes;
        }

        public bool Contains(Rect2 _other)
        {
            for (int i = 0; i < 4; i++)
            {
                if (!Contains(_other.points[i]))
                {
                    return false;
                }
            }

            return true;
        }

        public bool IntersectsLine(Vec2 a1, Vec2 b1)
        {
            if (Contains(a1) && Contains(b1))
                return true;

            for (int i = 0; i < 4; i++)
            {
                Vec2 intersect;
                if (Vec2.GetLineIntersection(points[i], points[(i + 1) % 4], a1, b1, out intersect))
                {
                    return true;
                }
            }

            return false;
        }

        public bool Intersects(Rect2 _other)
        {
            return SeparatingAxis.Intersects(this, _other);
        }

        public Vec2 GetIntersectPoint(Rect2 _other)
        {
            Vec2 result = _other.Points.FirstOrDefault(point => Contains(point));
            if( result != null )
            {
                return result;
            }

            result = points.FirstOrDefault(point => _other.Contains(point));
            if (result != null)
            {
                return result;
            }

            return _other.Center;
        }

        public override bool Equals(object obj)
        {
            Rect2 other = obj as Rect2;
            if (other == null) return false;
            for (int i = 0; i < 4; i++)
                if (points[i] != other.points[i])
                    return false;
            return true;
        }

        public override int GetHashCode()
        {
            if (hashCode == 0)
                hashCode = points[0].GetHashCode();
            return hashCode;
        }

        public override string ToString()
        {
            return base.ToString() + " ["
                + points[0].ToString() + ","
                + points[1].ToString() + ","
                + points[2].ToString() + ","
                + points[3].ToString() + "]";
        }

        private int hashCode;
        private Vec2[] points = new Vec2[4];
    }

    public struct AARect2
    {
        public static AARect2 UnitCube = new AARect2(-0.5f, -0.5f, 1.0f, 1.0f);

        public float Top;
        public float Left;
        public float Bottom;
        public float Right;

        public IEnumerable<Vec2> Points { get { yield return TopLeft; yield return TopRight; yield return BottomRight; yield return BottomLeft; } }

        public Vec2 TopLeft { get { return new Vec2(Left,Top); } }
        public Vec2 TopRight { get { return new Vec2(Right,Top); } }
        public Vec2 BottomLeft { get { return new Vec2(Left,Bottom); } }
        public Vec2 BottomRight { get { return new Vec2(Right,Bottom); } }

        public Vec2 Min { get { return TopLeft; } }
        public Vec2 Max { get { return BottomRight; } }

        public Vec2 Center { get { return new Vec2((Left + Right) / 2f, (Top + Bottom) / 2f); } }

        public float Width { get { return TopRight.X - TopLeft.X; } }
        public float Height { get { return BottomLeft.Z - TopLeft.Z; } }

        public AARect2(float _x, float _z, float _sizeX, float _sizeZ)
        {
            Left = _x;
            Top = _z;
            Right = _x + _sizeX;
            Bottom = _z + _sizeZ;            
        }

        public AARect2(AARect2 _other)
        {
            Left = _other.Left;
            Top = _other.Top;
            Right = _other.Right;
            Bottom = _other.Bottom;     
        }

        public AARect2(Vec2[] _points)
        {
            if (_points.Length != 4)
            {
                throw new Exception("[Rect2::Rect2] Invalid initializer array");
            }

            Left = _points[0].X;
            Top = _points[0].Z;
            Right = _points[3].X;
            Bottom = _points[3].Z;
        }

        public AARect2(Rect2 _other)
        {
            Left = _other.Points[0].X;
            Top = _other.Points[0].Z;
            Right = _other.Points[2].X;
            Bottom = _other.Points[2].Z;
        }

        public AARect2(Rect3 _other)
        {
            Left = _other.Points[0].X;
            Top = _other.Points[0].Z;
            Right = _other.Points[2].X;
            Bottom = _other.Points[2].Z;
        }

        public static bool operator ==(AARect2 r1, AARect2 r2)
        {
            return r1.Equals(r2);
        }

        public static bool operator !=(AARect2 r1, AARect2 r2)
        {
            return !r1.Equals(r2);
        }

        public bool Contains(Vec3 _point)
        {
            return Contains(new Vec2(_point.X, _point.Z));
        }

        public bool Contains(Vec2 _point)
        {
            return (_point.X >= Left) && (_point.X <= Right) && (_point.Z >= Top) && (_point.Z <= Bottom);
        }

        public bool Contains(Rect2 _other)
        {
            AARect2 thisRect = this;
            return !_other.Points.Any(point => !thisRect.Contains(point));
        }

        public bool Contains(AARect2 _other)
        {
            AARect2 thisRect = this;
            return !_other.Points.Any(point => !thisRect.Contains(point));
        }

        public bool Intersects(AARect2 _other)
        {
            return (Min.X <= _other.Max.X && Max.X >= _other.Min.X &&
                       Min.Z <= _other.Max.Z && Max.Z >= _other.Min.Z);
        }

        public bool Intersects(Rect2 _other)
        {            
            for (int i = 0; i < 4; i++)
            {
                if (Contains(_other.Points[i]))
                {
                    return true;
                }
            }

            if (_other.Contains(TopLeft) || _other.Contains(TopRight) || _other.Contains(BottomLeft) || _other.Contains(BottomRight))
                return true;

            return SeparatingAxis.Intersects(new Rect2(this), _other);
        }

        // This function is not always accurate.  USE AT OWN RISK
        public bool IntersectsCheap(Rect2 _other)
        {
            for (int i = 0; i < 4; i++)
            {
                if (Contains(_other.Points[i]))
                {
                    return true;
                }
            }

            if (_other.Contains(TopLeft) || _other.Contains(TopRight) || _other.Contains(BottomLeft) || _other.Contains(BottomRight))
                return true;

            return false;
        }

        public bool IntersectsLine(Vec2 _pointA, Vec2 _pointB)
        {
            //return CohenSutherland.LineIntersectsAARect(_pointA, _pointB, this);
            return IntersectsLineAlt(_pointA, _pointB);
        }

        // a1 is line1 start, a2 is line1 end, b1 is line2 start, b2 is line2 end
        public bool IntersectsLineAlt(Vec2 _pointA1, Vec2 _pointA2)
        {       
            Vec2 intersect;
            if( GetIntersection(_pointA1, _pointA2, TopLeft, TopRight, out intersect) )
            {
                return true;
            }
            if( GetIntersection(_pointA1, _pointA2, TopRight, BottomRight, out intersect) )
            {
                return true;
            }
            if( GetIntersection(_pointA1, _pointA2, BottomRight, BottomLeft, out intersect) )
            {
                return true;
            }
            if( GetIntersection(_pointA1, _pointA2, BottomLeft, TopLeft, out intersect) )
            {
                return true;
            }

            return false;
        }

        private bool GetIntersection(Vec2 _pointA1, Vec2 _pointA2, Vec2 _pointB1, Vec2 _pointB2, out Vec2 _intersect)
        {
            Vec2 b = _pointA2 - _pointA1;
            Vec2 d = _pointB2 - _pointB1;
            float bDotDPerp = b.X * d.Z - b.Z * d.X;

            // if b dot d == 0, it means the lines are parallel so have infinite intersection points
            if (bDotDPerp == 0)
            {
                _intersect = Vec2.Zero;
                return false;
            }

            Vec2 c = _pointB1 - _pointA1;
            float t = (c.X * d.Z - c.Z * d.X) / bDotDPerp;
            if (t < 0 || t > 1)
            {
                _intersect = Vec2.Zero;
                return false;
            }

            float u = (c.X * b.Z - c.Z * b.X) / bDotDPerp;
            if (u < 0 || u > 1)
            {
                _intersect = Vec2.Zero;
                return false;
            }

            _intersect = _pointA1 + t * b;
            return true;
        }
        
        public Vec2 GetIntersectPoint(Rect2 _other)
        {
            return _other.GetIntersectPoint(new Rect2(this));            
        }

        public bool GetIntersectPoint(Vec2 _start, Vec2 _end, out Vec2 _intersectPoint)
        {
            Vec2 intersect;
            if( GetIntersection(_start, _end, TopLeft, TopRight, out intersect) )
            {
                _intersectPoint = intersect;
                return true;
            }
            if( GetIntersection(_start, _end, TopRight, BottomRight, out intersect) )
            {
                _intersectPoint = intersect;
                return true;
            }
            if( GetIntersection(_start, _end, BottomRight, BottomLeft, out intersect) )
            {
                _intersectPoint = intersect;
                return true;
            }
            if( GetIntersection(_start, _end, BottomLeft, TopLeft, out intersect) )
            {
                _intersectPoint = intersect;
                return true;
            }

            _intersectPoint = Vec2.Zero;
            return false;
        }

        public IEnumerable<AARect2> Subdivide()
        {
            float curX = TopLeft.X;
            float curY = TopLeft.Z;
            float size = (TopRight.X - TopLeft.X) / 2;

            for (int x = 0; x < 2; x++)
            {
                for (int y = 0; y < 2; y++)
                {
                    yield return new AARect2(curX, curY, size, size);
                    curY += size;
                }
                curY = TopLeft.Z;
                curX += size;
            }
        }

        public bool IsAdjacent(AARect2 _rect2)
        {
            return Intersects(_rect2);
        }

        public static AARect2 operator +(AARect2 r, Vec2 v)
        {
            return new AARect2(r.Left + v.X, r.Top + v.Z, r.Width, r.Height);
                                            
        }

        public static AARect2 operator -(AARect2 r, Vec2 v)
        {
            return new AARect2(r.Left - v.X, r.Top - v.Z, r.Width, r.Height);
        }

        public override bool Equals(object obj)
        {
            AARect2 other = (AARect2)obj;
            return other.Min.Equals(Min) && other.Max.Equals(Max);
        }

        public override int GetHashCode()
        {
            return Min.GetHashCode() ^ Max.GetHashCode();
        }
    }

    public class Rect3
    {
        public static Rect3 UnitCube = new Rect3(-0.5f, -0.5f, -0.5f, 1.0f, 1.0f, 1.0f);
        public static Rect3 Zero = new Rect3(0,0,0,0,0,0);

        // Because of how we use float.LimitPrecision to avoid floating-point
        // equality comparison failures due to floating-point rounding errors,
        // these must be truly read-only!

        public Vec3[] Points
        {
            get
            {
                return points;
            }
        }
        public Vec3 UpperTopLeft { get { return points[0]; } }
        public Vec3 UpperTopRight { get { return points[1]; } }
        public Vec3 UpperBottomLeft { get { return points[2]; } }
        public Vec3 UpperBottomRight { get { return points[3]; } }
        public Vec3 LowerTopLeft { get { return points[4]; } }
        public Vec3 LowerTopRight { get { return points[5]; } }
        public Vec3 LowerBottomLeft { get { return points[6]; } }
        public Vec3 LowerBottomRight { get { return points[7]; } }
        public Vec3 Center
        {
            get
            {
                return new Vec3(
                    UpperTopLeft.X + (UpperTopRight.X - UpperTopLeft.X) / 2f, 
                    UpperTopLeft.Y + (LowerTopLeft.Y - UpperTopLeft.Y) / 2f, 
                    UpperTopLeft.Z + (UpperBottomLeft.Z - UpperTopLeft.Z) / 2f);
            }
        }

        public Rect3()
        {
        }

        public Rect3(float _x, float _y, float _z, float _sizeX, float _sizeY, float _sizeZ)
        {
            points = new Vec3[8];
            points[0] = new Vec3(_x, _y, _z);
            points[1] = new Vec3(_x + _sizeX, _y, _z);
            points[2] = new Vec3(_x + _sizeX, _y, _z + _sizeZ);
            points[3] = new Vec3(_x, _y, _z + _sizeZ);
            points[4] = new Vec3(_x, _y + _sizeY, _z);
            points[5] = new Vec3(_x + _sizeX, _y + _sizeY, _z);
            points[6] = new Vec3(_x + _sizeX, _y + _sizeY, _z + _sizeZ);
            points[7] = new Vec3(_x, _y + _sizeY, _z + _sizeZ);
        }

        public Rect3(Vec3[] _points)
        {
            if (_points.Length != 8)
            {
                throw new Exception("[Rect3::Rect3] Invalid initializer array");
            }

            points = new Vec3[8];
            for(int i = 0; i < 8; i++)
                points[i] = _points[i];
        }

        public Rect3(Rect3 other)
        {
            points = new Vec3[8];
            for (int i = 0; i < 8; i++)
            {
                points[i] = other.Points[i];
            }
        }

        public Rect3(string xmlString)
        {
            points = new Vec3[8];

            int pointIndex = 0;
            foreach (var vecString in xmlString.Split(new string[] { ")(" }, new StringSplitOptions()))
            {
                string[] vecComps = vecString.Split(new char[] { ',' });
                points[pointIndex++] = new Vec3(float.Parse(vecComps[0].Replace("[(", "")), float.Parse(vecComps[1]), float.Parse(vecComps[2].Replace(")]", "")));
            }
        }

        public Rect3 Add(Vec3 v)
        {
            return new Rect3(new Vec3[] { new Vec3(UpperTopLeft.X + v.X, UpperTopLeft.Y + v.Y, UpperTopLeft.Z + v.Z),
                             new Vec3(UpperTopRight.X + v.X, UpperTopRight.Y + v.Y, UpperTopRight.Z + v.Z),
                             new Vec3(UpperBottomLeft.X + v.X, UpperBottomLeft.Y + v.Y, UpperBottomLeft.Z + v.Z),
                             new Vec3(UpperBottomRight.X + v.X, UpperBottomRight.Y + v.Y, UpperBottomRight.Z + v.Z),
                             new Vec3(LowerTopLeft.X + v.X, LowerTopLeft.Y + v.Y, LowerTopLeft.Z + v.Z),
                             new Vec3(LowerTopRight.X + v.X, LowerTopRight.Y + v.Y, LowerTopRight.Z + v.Z),
                             new Vec3(LowerBottomLeft.X + v.X, LowerBottomLeft.Y + v.Y, LowerBottomLeft.Z + v.Z),
                             new Vec3(LowerBottomRight.X + v.X, LowerBottomRight.Y + v.Y, LowerBottomRight.Z + v.Z) });
        }

        public Rect2 Flatten()
        {
            return new Rect2(points.Take(4).Select(item => new Vec2(item)).ToArray());
        }

        public float GetMaxY()
        {
            return points.OrderByDescending(item => item.Y).First().Y;
        }

        // MSM TODO: put this somewhere else
        public static bool IsInside(float _item, float _start, float _end)
        {
            float start = _start < _end ? _start : _end;
            float end = _start < _end ? _end : _start;

            return _item >= start && _item <= end;
        }

        // DAB NOTE: This only works on axis aligned cubes!
        // MSM TODO: FIX THIS
        public bool Contains(Vec3 _point)
        {
            return
                IsInside(_point.X, points[0].X, points[1].X) &&
                IsInside(_point.Y, points[0].Y, points[4].Y) &&
                IsInside(_point.Z, points[0].Z, points[3].Z);
        }

        public override bool Equals(object obj)
        {
            Rect3 other = obj as Rect3;
            if (other == null) return false;
            for (int i = 0; i < 8; i++)
                if (points[i] != other.points[i])
                    return false;
            return true;
        }

        public override int GetHashCode()
        {
            if (hashCode == 0)
                hashCode = points[0].GetHashCode();
            return hashCode;
        }

        public override string ToString()
        {
            return base.ToString() + " ["
                + points[0].ToString() + ","
                + points[1].ToString() + ","
                + points[2].ToString() + ","
                + points[3].ToString() + "]";
        }

        private int hashCode;

        private Vec3[] points { get; set; } 
    }

    public class Mat4
    {
        private readonly float[,] _matrix = new float[4,4];

        public Mat4()
        {
            this.MakeIdentity();
        }

        public Mat4(float[,] _values)
        {
            _matrix =  _values;
        }

        public Mat4 MakeIdentity()
        {
            this._matrix[0, 0] = this._matrix[1, 1] = this._matrix[2, 2] = this._matrix[3, 3] = 1;
            this._matrix[0, 1] = this._matrix[0, 2] = this._matrix[0, 3] =
                                 this._matrix[1, 0] = this._matrix[1, 2] = this._matrix[1, 3] =
                                                      this._matrix[2, 0] = this._matrix[2, 1] = this._matrix[2, 3] =
                                                                           this._matrix[3, 0] = this._matrix[3, 1] = this._matrix[3, 2] = 0;
            return this;
        }

        public void SetBy(Mat4 matrix)
        {
            for (var i = 0; i < 4; i++)
            {
                for (var j = 0; j < 4; j++)
                {
                    this._matrix[i, j] = matrix._matrix[i, j];
                }
            }
        }

        public Mat4 GetInverse()
        {
            var s0 = _matrix[0, 0] * _matrix[1, 1] - _matrix[1, 0] * _matrix[0, 1];
            var s1 = _matrix[0, 0] * _matrix[1, 2] - _matrix[1, 0] * _matrix[0, 2];
            var s2 = _matrix[0, 0] * _matrix[1, 3] - _matrix[1, 0] * _matrix[0, 3];
            var s3 = _matrix[0, 1] * _matrix[1, 2] - _matrix[1, 1] * _matrix[0, 2];
            var s4 = _matrix[0, 1] * _matrix[1, 3] - _matrix[1, 1] * _matrix[0, 3];
            var s5 = _matrix[0, 2] * _matrix[1, 3] - _matrix[1, 2] * _matrix[0, 3];

            var c5 = _matrix[2, 2] * _matrix[3, 3] - _matrix[3, 2] * _matrix[2, 3];
            var c4 = _matrix[2, 1] * _matrix[3, 3] - _matrix[3, 1] * _matrix[2, 3];
            var c3 = _matrix[2, 1] * _matrix[3, 2] - _matrix[3, 1] * _matrix[2, 2];
            var c2 = _matrix[2, 0] * _matrix[3, 3] - _matrix[3, 0] * _matrix[2, 3];
            var c1 = _matrix[2, 0] * _matrix[3, 2] - _matrix[3, 0] * _matrix[2, 2];
            var c0 = _matrix[2, 0] * _matrix[3, 1] - _matrix[3, 0] * _matrix[2, 1];

            // Should check for 0 determinant
            float invdet = 1.0f / (s0 * c5 - s1 * c4 + s2 * c3 + s3 * c2 - s4 * c1 + s5 * c0);

            var b = new float[4, 4];

            b[0, 0] = (_matrix[1, 1] * c5 - _matrix[1, 2] * c4 + _matrix[1, 3] * c3) * invdet;
            b[0, 1] = (-_matrix[0, 1] * c5 + _matrix[0, 2] * c4 - _matrix[0, 3] * c3) * invdet;
            b[0, 2] = (_matrix[3, 1] * s5 - _matrix[3, 2] * s4 + _matrix[3, 3] * s3) * invdet;
            b[0, 3] = (-_matrix[2, 1] * s5 + _matrix[2, 2] * s4 - _matrix[2, 3] * s3) * invdet;

            b[1, 0] = (-_matrix[1, 0] * c5 + _matrix[1, 2] * c2 - _matrix[1, 3] * c1) * invdet;
            b[1, 1] = (_matrix[0, 0] * c5 - _matrix[0, 2] * c2 + _matrix[0, 3] * c1) * invdet;
            b[1, 2] = (-_matrix[3, 0] * s5 + _matrix[3, 2] * s2 - _matrix[3, 3] * s1) * invdet;
            b[1, 3] = (_matrix[2, 0] * s5 - _matrix[2, 2] * s2 + _matrix[2, 3] * s1) * invdet;

            b[2, 0] = (_matrix[1, 0] * c4 - _matrix[1, 1] * c2 + _matrix[1, 3] * c0) * invdet;
            b[2, 1] = (-_matrix[0, 0] * c4 + _matrix[0, 1] * c2 - _matrix[0, 3] * c0) * invdet;
            b[2, 2] = (_matrix[3, 0] * s4 - _matrix[3, 1] * s2 + _matrix[3, 3] * s0) * invdet;
            b[2, 3] = (-_matrix[2, 0] * s4 + _matrix[2, 1] * s2 - _matrix[2, 3] * s0) * invdet;

            b[3, 0] = (-_matrix[1, 0] * c3 + _matrix[1, 1] * c1 - _matrix[1, 2] * c0) * invdet;
            b[3, 1] = (_matrix[0, 0] * c3 - _matrix[0, 1] * c1 + _matrix[0, 2] * c0) * invdet;
            b[3, 2] = (-_matrix[3, 0] * s3 + _matrix[3, 1] * s1 - _matrix[3, 2] * s0) * invdet;
            b[3, 3] = (_matrix[2, 0] * s3 - _matrix[2, 1] * s1 + _matrix[2, 2] * s0) * invdet;

            return new Mat4(b);
        }

        public static Mat4 NewRotateAroundX(double radians)
        {
            var matrix = new Mat4();
            matrix._matrix[1, 1] = (float)Math.Cos(radians);
            matrix._matrix[1, 2] = (float)Math.Sin(radians);
            matrix._matrix[2, 1] = (float)-(Math.Sin(radians));
            matrix._matrix[2, 2] = (float)Math.Cos(radians);
            return matrix;
        }
        public static Mat4 NewRotateAroundY(double radians)
        {
            var matrix = new Mat4();
            matrix._matrix[0, 0] = (float)Math.Cos(radians);
            matrix._matrix[0, 2] = (float)-(Math.Sin(radians));
            matrix._matrix[2, 0] = (float)Math.Sin(radians);
            matrix._matrix[2, 2] = (float)Math.Cos(radians);
            return matrix;
        }
        public static Mat4 NewRotateAroundZ(double radians)
        {
            var matrix = new Mat4();
            matrix._matrix[0, 0] = (float)Math.Cos(radians);
            matrix._matrix[0, 1] = (float)Math.Sin(radians);
            matrix._matrix[1, 0] = (float)-(Math.Sin(radians));
            matrix._matrix[1, 1] = (float)Math.Cos(radians);
            return matrix;
        }

        public static Mat4 NewRotate(double radiansX, double radiansY, double radiansZ)
        {
            var matrix = NewRotateAroundX(radiansX);
            matrix = matrix * NewRotateAroundY(radiansY);
            matrix = matrix * NewRotateAroundZ(radiansZ);
            return matrix;
        }

        public static Mat4 NewRotateByDegrees(float degreesX, float degreesY, float degreesZ)
        {
            return NewRotate(
                        degreesX.ToRadians(), 
                        degreesY.ToRadians(), 
                        degreesZ.ToRadians()
                   );
        }

        public static Mat4 NewRotateFromDegreesAroundX(float degrees)
        {
            return NewRotateAroundX(degrees.ToRadians());
        }
        public static Mat4 NewRotateFromDegreesAroundY(float degrees)
        {
            return NewRotateAroundY(degrees.ToRadians());
        }
        public static Mat4 NewRotateFromDegreesAroundZ(float degrees)
        {
            return NewRotateAroundZ(degrees.ToRadians());
        }

        public static Mat4 NewTranslate(float distX, float distY, float distZ)
        {
            var matrix = new Mat4();
            matrix._matrix[0, 3] = distX;
            matrix._matrix[1, 3] = distY;
            matrix._matrix[2, 3] = distZ;
            return matrix;
        }

        public static Mat4 operator *(Mat4 matrix1, Mat4 matrix2)
        {
            var matrix = new Mat4();
            for(var i = 0; i < 4; i++)
            {
                for(var j = 0; j < 4; j++)
                {
                    matrix._matrix[i, j] = 
                        (matrix2._matrix[i, 0] * matrix1._matrix[0, j]) +
                        (matrix2._matrix[i, 1] * matrix1._matrix[1, j]) +
                        (matrix2._matrix[i, 2] * matrix1._matrix[2, j]) +
                        (matrix2._matrix[i, 3] * matrix1._matrix[3, j]);
                }
            }
            return matrix;
        }

        public static Vec3 operator *(Mat4 matrix1, Vec3 vec)
        {
            var x = vec.X * matrix1._matrix[0, 0] +
                    vec.Y * matrix1._matrix[0, 1] +
                    vec.Z * matrix1._matrix[0, 2] +
                    matrix1._matrix[0,3];
            var y = vec.X * matrix1._matrix[1, 0] +
                    vec.Y * matrix1._matrix[1, 1] +
                    vec.Z * matrix1._matrix[1, 2] +
                    matrix1._matrix[1, 3]; ;
            var z = vec.X * matrix1._matrix[2, 0] +
                    vec.Y * matrix1._matrix[2, 1] +
                    vec.Z * matrix1._matrix[2, 2] +
                    matrix1._matrix[2, 3]; ;
            
            return new Vec3(x,y,z);
        }

        public override string ToString()
        {
            string result = "";
            for(var i = 0; i < 4; i++)
            {
                string row = "(";
                for(var j = 0; j < 4; j++)
                {
                    row = row + _matrix[i,j] + ",";
                }
                result = result + row + "), ";
            }
            return result;
        }
    }

    public static class SeparatingAxis
    {
        public static bool Intersects(Rect2 r1, Rect2 r2)
        {
            Vec2[] normals = new Vec2[] 
		    {
			    GetNormal(r1.TopLeft,r1.TopRight),
			    GetNormal(r1.TopLeft,r1.BottomLeft),
			    GetNormal(r2.TopLeft,r2.TopRight),
			    GetNormal(r2.TopLeft,r2.BottomLeft),
		    };

            float P1_max, P2_max, Q1_max, Q2_max, R1_max, R2_max, S1_max, S2_max,
                  P1_min, P2_min, Q1_min, Q2_min, R1_min, R2_min, S1_min, S2_min;

            GetMinMax(r1.Points, normals[1], out P1_min, out P1_max);
            GetMinMax(r2.Points, normals[1], out P2_min, out P2_max);
            GetMinMax(r1.Points, normals[0], out Q1_min, out Q1_max);
            GetMinMax(r2.Points, normals[0], out Q2_min, out Q2_max);

            GetMinMax(r1.Points, normals[3], out R1_min, out R1_max);
            GetMinMax(r2.Points, normals[3], out R2_min, out R2_max);
            GetMinMax(r1.Points, normals[2], out S1_min, out S1_max);
            GetMinMax(r2.Points, normals[2], out S2_min, out S2_max);

            bool separate_p = P1_max < P2_min || P2_max < P1_min;
            bool separate_Q = Q1_max < Q2_min || Q2_max < Q1_min;
            bool separate_R = R1_max < R2_min || R2_max < R1_min;
            bool separate_S = S1_max < S2_min || S2_max < S1_min;

            return !(separate_p || separate_Q || separate_R || separate_S);
        }

        public static Vec2 GetNormal(Vec2 _pos1, Vec2 _pos2)
        {
            float dz = _pos2.Z - _pos1.Z;
            float dx = _pos2.X - _pos1.X;

            return new Vec2(-dz, dx);
        }

        public static void GetMinMax(Vec2[] _vecs, Vec2 _axis, out float _min, out float _max)
        {
            _min = _max = _vecs[0].DotProduct(_axis);

            for (int i = 1; i < _vecs.Length; i++)
            {
                float cur_proj = _vecs[i].DotProduct(_axis);
                if (_min > cur_proj)
                {
                    _min = cur_proj;
                }
                if (cur_proj > _max)
                {
                    _max = cur_proj;
                }
            }
        }
    }
}