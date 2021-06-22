using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace xAudit
{
    public struct Version : IEquatable<Version>, IComparable<Version>, IComparer<Version>
    {

        public int Major { get; set; }
        public int Minor { get; set; }
        public int Patch { get; set; }
        private Version(int Major, int Minor, int Patch)
        {
            this.Major = Major;
            this.Minor = Minor;
            this.Patch = Patch;
        }
        public static implicit operator Version(string v)
        {
            if (string.IsNullOrWhiteSpace(v))
                return new Version();
            var items = v.Split('.');
            if (items.Length != 3)
                throw new InvalidCastException("The provided version must include exactly three parts(major, minor & patch)");
            if (!int.TryParse(items[0], out int mj))
                throw new InvalidCastException("All parts of a version should be a whole number");
            if (!int.TryParse(items[1], out int mn))
                throw new InvalidCastException("All parts of a version should be a whole number");
            if (!int.TryParse(items[2], out int p))
                throw new InvalidCastException("All parts of a version should be a whole number");
            return new Version(mj, mn, p);
        }
        public static bool operator >(Version lhs, Version rhs)
        {
            //query:  lhs > rhs
            //example : 1.2.3 > 1.0.3

            if (lhs.Major > rhs.Major)
                return true;
            if (lhs.Major < rhs.Major)
                return false;
            if (lhs.Minor > rhs.Minor)
                return true;
            if (lhs.Minor < rhs.Minor)
                return false;
            if (lhs.Patch > rhs.Patch)
                return true;
            if (lhs.Patch < rhs.Patch)
                return false;
            return false;
        }
        public static bool operator <(Version lhs, Version rhs)
        {
            return rhs > lhs;
        }
        public static bool operator >=(Version lhs, Version rhs)
        {
            return lhs > rhs || lhs == rhs;
        }
        public static bool operator <=(Version lhs, Version rhs)
        {
            return lhs < rhs || lhs == rhs;
        }
        public static bool operator ==(Version lhs, Version rhs)
        {
            if ((lhs.Major == rhs.Major) && (lhs.Minor == rhs.Minor) && (lhs.Patch == rhs.Patch))
                return true;
            return false;
        }
        public static bool operator !=(Version lhs, Version rhs)
        {
            return !(lhs == rhs);
        }
        public override string ToString()
        {
            return $"{Major}.{Minor}.{Patch}";
        }

        public bool Equals(Version other)
        {
            return this == other;
        }

        public int CompareTo(Version other)
        {
            if (this > other)
                return 1;
            if (this < other)
                return -1;
            return 0;
        }
        public override bool Equals(object obj)
        {
            return this == (Version)obj;
        }
        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + this.Major.GetHashCode();
                hash = hash * 23 + this.Minor.GetHashCode();
                hash = hash * 23 + this.Patch.GetHashCode();
                return hash;
            }
        }
        /// <summary>
        /// Will return the previous lowest version relative to this version among the list of versions passed 
        /// </summary>
        /// <param name="versions">The list of versions from which the output is computed</param>
        /// <returns></returns>
        public Version FindImmediatePrevious(params Version[] versions)
        {
            var self = this;
            return versions.ToList().Where(x => x < self).Max();
        }
        /// <summary>
        /// Will return the next highest version relative to this version among the list of versions passed 
        /// </summary>
        /// <param name="versions">The list of versions from which the output is computed</param>
        /// <returns></returns>
        public Version FindImmediateNext(params Version[] versions)
        {
            var self = this;
            return versions.ToList().Where(x => x > self).Min();
        }
        public int Compare(Version x, Version y)
        {
            if (x > y)
                return 1;
            if (x < y)
                return -1;
            return 0;
        }
    }
}
