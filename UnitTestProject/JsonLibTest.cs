using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace UnitTestProject
{
    [TestClass]
    public class JsonLibTest
    {
        [TestMethod]
        public void ObjectToJsonConvertTest()
        {
            // arrange
            TestJson json1 = new TestJson
            {
                Name = "name1",
                Count = 5
            };
            JObject jo1 = JObject.FromObject(json1);

            // act
            TestJson json2 = JsonConvert.DeserializeObject<TestJson>(jo1.ToString());

            // assert
            Assert.AreEqual<TestJson>(json1, json2);
        }

        [TestMethod]
        public void ListToJsonConvert()
        {
            // arrange
            List<TestJson> list = new List<TestJson>
            {
                new TestJson { Name = "name1", Count = 1 },
                new TestJson { Name = "name2", Count = 2 },
                new TestJson { Name = "name3", Count = 3 }
            };
            JArray jarray = JArray.FromObject(list);

            // act
            List<TestJson> list_actual = JsonConvert.DeserializeObject<List<TestJson>>(jarray.ToString());

            // assert
            CollectionAssert.AreEqual(list, list_actual);
        }
    }
    public class TestJson
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("count")]
        public int Count { get; set; }
        public override int GetHashCode()
        {
            return Count;
        }
        public override bool Equals(object obj)
        {
            if (obj == null) return false;
            TestJson objAsTestJson = obj as TestJson;
            if (objAsTestJson == null)
            {
                return false;
            }
            return (this.Name == objAsTestJson.Name &&
                    this.Count == objAsTestJson.Count);
        }
    }

}
