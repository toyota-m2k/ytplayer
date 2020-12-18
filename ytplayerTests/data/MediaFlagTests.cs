using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using ytplayer.data;

namespace ytplayerTests.data {
    [TestClass]
    public class MediaFlagTests {
        [TestMethod]
        public void MediaFlagTest() {
            MediaFlag f = MediaFlag.NONE;
            Assert.IsFalse(f.HasVideo());
            Assert.IsFalse(f.HasAudio());

            f = f.PlusVideo();
            Assert.IsTrue(f.HasVideo());
            Assert.IsFalse(f.HasAudio());
            Assert.AreEqual(MediaFlag.VIDEO, f);

            f = f.PlusAudio();
            Assert.IsTrue(f.HasVideo());
            Assert.IsTrue(f.HasAudio());
            Assert.AreEqual(MediaFlag.BOTH, f);

            f = f.MinusVideo();
            Assert.IsFalse(f.HasVideo());
            Assert.IsTrue(f.HasAudio());
            Assert.AreEqual(MediaFlag.AUDIO, f);

            f = f.PlusVideo();
            Assert.IsTrue(f.HasVideo());
            Assert.IsTrue(f.HasAudio());
            Assert.AreEqual(MediaFlag.BOTH, f);

            f = f.MinusAudio();
            Assert.IsTrue(f.HasVideo());
            Assert.IsFalse(f.HasAudio());
            Assert.AreEqual(MediaFlag.VIDEO, f);

            f = f.MinusVideo();
            Assert.IsFalse(f.HasVideo());
            Assert.IsFalse(f.HasAudio());
            Assert.AreEqual(MediaFlag.NONE, f);

        }
    }
}
