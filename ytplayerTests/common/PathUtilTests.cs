using Microsoft.VisualStudio.TestTools.UnitTesting;
using ytplayer.common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ytplayer.common.Tests {
    [TestClass()]
    public class PathUtilTests {
        [TestMethod()]
        public void normalizeDirnameTest() {
            Assert.AreEqual("c:\\temp\\abc", PathUtil.normalizeDirname("c:\\temp\\abc\\"));
            Assert.AreEqual("c:\\temp\\abc", PathUtil.normalizeDirname("c:\\temp\\abc"));
            Assert.AreEqual("c:\\temp\\abc", PathUtil.normalizeDirname("  c:\\temp\\abc\t\n"));
            Assert.AreEqual("c:\\temp\\abc", PathUtil.normalizeDirname("c:/temp/abc"));
            Assert.AreEqual("c:\\temp\\abc", PathUtil.normalizeDirname("c:/temp/abc/"));
        }

        [TestMethod()]
        public void isEqualDirectoryNameTest() {
            Assert.IsTrue(PathUtil.isEqualDirectoryName("c:\\teMp\\abc", "C:/temp/Abc/"));
            Assert.IsTrue(PathUtil.isEqualDirectoryName("c:\\teMp\\abc", "  C:/temp/Abc/"));
            Assert.IsTrue(PathUtil.isEqualDirectoryName("c:\\teMp\\abc\\", "  C:/temp/Abc"));
            Assert.IsFalse(PathUtil.isEqualDirectoryName("c:\\temp\\abc", "C:/temp /Abc/"));
        }

        [TestMethod()]
        public void isExistsTest() {
            Assert.IsTrue(PathUtil.isExists(Environment.CurrentDirectory));
        }

        [TestMethod()]
        public void isDirectoryTest() {
            Assert.IsTrue(PathUtil.isDirectory(Environment.CurrentDirectory));
        }

        [TestMethod()]
        public void appendPathStringTest() {
            //var orgPath = "C:\\Windows\\system32;C:\\Windows;C:\\Windows\\System32\\Wbem;C:\\Windows\\System32\\WindowsPowerShell\\v1.0\\;C:\\Windows\\System32\\OpenSSH\\;C:\\Program Files\\Microsoft VS Code\\bin;C:\\Program Files\\TortoiseSVN\\bin;C:\\Program Files\\Microsoft SQL Server\\130\\Tools\\Binn\\;C:\\Program Files\\Microsoft SQL Server\\Client SDK\\ODBC\\170\\Tools\\Binn\\;C:\\Program Files\\Git\\cmd;C:\\Program Files (x86)\\Windows Kits\\10\\Windows Performance Toolkit\\;C:\\Users\\mitsu\\AppData\\Roaming\\nvm;C:\\Program Files\\nodejs;C:\\Program Files (x86)\\JustSystems\\JSLIB32;C:\\HashiCorp\\Vagrant\\bin;C:\\Program Files\\PuTTY\\;C:\\Program Files\\Docker\\Docker\\resources\\bin;C:\\ProgramData\\DockerDesktop\\version-bin;C:\\Program Files\\dotnet\\;C:\\bin\\Ruby26-x64\\bin;C:\\Users\\mitsu\\AppData\\Local\\Microsoft\\WindowsApps;C:\\Users\\mitsu\\.dotnet\\tools;C:\\bin\\Ruby26-x64\\msys64\\usr\\bin;C:\\Users\\mitsu\\AppData\\Local\\Programs\\Fiddler;c:\\bin\\tools;C:\\Users\\mitsu\\.dotnet\\tools";
            var orgPath = "C:\\Program Files\\Microsoft VS Code\\bin;C:\\Program Files\\TortoiseSVN\\bin;C:\\Users\\mitsu\\.dotnet\\tools";
            var p2 = PathUtil.appendPathString(orgPath, "x:\\hoge\\fuga");
            Assert.AreEqual(orgPath + ";" + "x:\\hoge\\fuga", p2);
            var p3 = PathUtil.appendPathString(p2, "x:\\hoge\\fuga");
            Assert.AreEqual(orgPath + ";" + "x:\\hoge\\fuga", p2);
            p2 = PathUtil.appendPathString(orgPath, "x:\\hoge\\fuga", "x:\\hoge\\fuga");
            Assert.AreEqual(orgPath + ";" + "x:\\hoge\\fuga", p2);
            p2 = PathUtil.appendPathString(orgPath, "x:\\hoge\\fuga", "x:/hoge/fuga/");
            Assert.AreEqual(orgPath + ";" + "x:\\hoge\\fuga", p2);
            p2 = PathUtil.appendPathString(orgPath, "x:\\hoge\\fuga", "y:/moge/piyo/");
            Assert.AreEqual(orgPath + ";" + "x:\\hoge\\fuga;y:\\moge\\piyo", p2);
            p2 = PathUtil.appendPathString(orgPath, "C:\\Program Files\\TortoiseSVN\\bin");
            Assert.AreEqual(orgPath, p2);
        }
    }
}