using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace ytplayerTests.common {
    [TestClass()]
    public class UtilsTest {
        [TestMethod()]
        public void FlagTest() {
            Flag f = Flag.BC;
            Assert.AreEqual(Flag.BC, f);
            Assert.IsFalse(f.IsA());
            f.SetA();
            Assert.AreEqual(Flag.ABC, f);
            Assert.AreEqual(Flag.BC, f.ResetA());
            Assert.AreEqual(Flag.BC, f);
        }
    }

    [Flags]
    public enum Flag {
        A = 1,
        B = 2,
        C = 4,
        AB = 3,
        AC = 5,
        BC = 6,
        ABC = 7,
    }

    public static class FlagExt {
        public static bool HasFlag(this Flag v, Flag flag) {
            return (v & flag) == flag;
        }
        public static bool IsA(this Flag v) {
            return v.HasFlag(Flag.A);
        }
        public static Flag SetA(this ref Flag v) {
            v |= Flag.A;
            return v;
        }
        public static Flag ResetA(this ref Flag v) {
            v &= ~Flag.A;
            return v;
        }
    }
}
