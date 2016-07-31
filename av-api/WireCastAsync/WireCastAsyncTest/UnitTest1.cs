using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WireCastAsync;
using System.Dynamic;
using System.Collections.Generic;

namespace WireCastAsyncTest
{
    [TestClass]
    public class UnitTest1
    {
        private static WirecastBinding binding;

        [ClassInitialize]
        public static void init(TestContext context)
        {
            binding = new WirecastBinding();
            Assert.IsNotNull(binding);
        }

        [TestMethod]
        public void TestGet()
        {
            Wirecast expected = new Wirecast();
            var actual = binding.Get();
            Assert.AreEqual(expected.isBroadcasting, actual.isBroadcasting);
            Assert.AreEqual(expected.isRecording, actual.isRecording);
        }

        [TestMethod]
        public void TestSet()
        {
            Wirecast before = binding.Get();            
            Assert.AreEqual(false, before.isRecording);

            ExpandoObject input = new ExpandoObject();
            IDictionary<string, object> json = input as IDictionary<string, object>;
            json.Add("isRecording", true);
            json.Add("isBroadcasting", false);
            json.Add("previewShotId", 0);
            json.Add("programShotId", 2);
            Wirecast after = binding.Set(input);
            Assert.AreEqual(true, after.isRecording);
            json.Remove("isRecording");
            json.Add("isRecording", false);
            after = binding.Set(input);
            Assert.AreEqual(false, after.isRecording);
        }

        [TestMethod]
        public void ParseTest()
        {
            ExpandoObject input = new ExpandoObject();
            IDictionary<string, object> json = input as IDictionary<string, object>;
            json.Add("isRecording", false);            
            json.Add("isBroadcasting", true);
            json.Add("previewShotId", 0);
            json.Add("programShotId", 2);
            
            Wirecast actual = binding.Parse((ExpandoObject)input);
            Assert.IsFalse(actual.isRecording);
            Assert.IsTrue(actual.isBroadcasting);
            Assert.AreEqual(0, actual.previewShotId);
            Assert.AreEqual(2, actual.programShotId);
        }
    }
}
