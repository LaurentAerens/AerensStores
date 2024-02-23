using Moq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AerensStoreTest
{
    [TestFixture]
    internal class KeyValueStoreTestWithTime
    {
        private KeyValueStore _store;
        private string _filePath;
        private DateTime _now = DateTime.Now;
        string testKey = "testKey";
        string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "testKeyValueStore.json");
        int defaultCleanUpTime = 3;

        [SetUp]
        public void SetUp()
        {

            DeltaTime _deltaTime1year = new DeltaTime(years: 1);
            // base test are done with 1 year
            _store = new KeyValueStore(path, _deltaTime1year, OverwriteSetting: true);
        }
        [TearDown]
        public void TearDown()
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        [Test]
        public void KeyValueStore_WhenCreated_CreatesFile()
        {
            _store.SetInt(testKey, 123);
            Assert.That(File.Exists(path), Is.True);
        }
        [Test]
        public void Set_WhenCalled_SetsValueForKey()
        {
            _store.Set(testKey, "testValue");

            var result = _store.Get(testKey);

            Assert.That(result, Is.EqualTo("testValue"));
        }
        [Test]
        public void Get_WhenNoDataIsSet_ReturnsNull()
        {
            var result = _store.Get("nonExistingTestKey");

            Assert.IsNull(result);
        }
        [Test]
        public void Get_WhenOlderMonthIsWithinDeltaTime_ReturnsOldValue()
        {
            string date = _now.AddMonths(-1).ToString("yyyyMM");
            string value = "testValue";
            CreateNewStore(date, value, path, new DeltaTime(months: 3));
            var result = _store.Get(testKey);
            Assert.That(result, Is.EqualTo(value));
        }
        [Test]
        public void Get_WhenOlderMonthIsntWithinDeltaTimeMonth_ReturnsNull()
        {
            string date = _now.AddMonths(-5).ToString("yyyyMM");
            string value = "testValue";
            CreateNewStore(date, value, path, new DeltaTime(months: 3));
            var result = _store.Get(testKey);
            Assert.IsNull(result);
        }
        [Test]
        public void Set_OverwritesKeyWithinDeltaTimeMonth_SetsValueForKey()
        {
            string date = _now.AddMonths(-1).ToString("yyyyMM");
            string wrongValue = "OldValue";
            CreateNewStore(date, wrongValue, path, new DeltaTime(months: 3));
            string correctValue = "CorrectValue";
            _store.Set(testKey, correctValue);

            var result = _store.Get(testKey);
            Assert.That(result, Is.EqualTo(correctValue));
        }
        [Test]
        public void Get_WhenOlderMonthIsntWithinDeltaTimeHour_ReturnsNull()
        {
            string date = _now.AddHours(-5).ToString("yyyyMMDDHH");
            string value = "testValue";
            CreateNewStore(date, value, path, new DeltaTime(hours: 3));
            var result = _store.Get(testKey);
            Assert.IsNull(result);
        }
        [Test]
        public void Set_OverwritesKeyWithinDeltaTimeHour_SetsValueForKey()
        {
            string date = _now.AddHours(-1).ToString("yyyyMMDDHH");
            string wrongValue = "OldValue";
            CreateNewStore(date, wrongValue, path, new DeltaTime(hours: 3));
            string correctValue = "CorrectValue";
            _store.Set(testKey, correctValue);

            var result = _store.Get(testKey);
            Assert.That(result, Is.EqualTo(correctValue));
        }
        [Test]
        public void Get_OldKeyWithIntervalHours_ReturnsOldValue()
        {
            string date = _now.AddMonths(-5).ToString("yyyyMM");
            string oldValue = "oldTestValue";
            string newValue = "newTestValue";
            CreateNewStore(date, oldValue, path, new DeltaTime(months: 3));
            _store.Set(testKey, newValue);
            var newResult = _store.Get(testKey);
            var oldResult = _store.Get(testKey, 1);
            Assert.That(newResult, Is.EqualTo(newValue));
            Assert.That(oldResult, Is.EqualTo(oldValue));
        }
        [Test]
        public void RemoveOldKeys_WhenCalled_RemovesOldKeys()
        {
            _store = new KeyValueStore(path, new DeltaTime(months: 1), OverwriteSetting: true);
            string date = _now.AddMonths(-5).ToString("yyyyMM");
            string oldValue = "testValue";
            CreateNewStore(date, oldValue, path, new DeltaTime(months: 1));
            string newValue = "newTestValue";
            string newKeyName = "newTestKey";
            _store.Set(newKeyName, newValue);
            _store = null; // reset store
            _store = new KeyValueStore(path);
            var Oldresult = _store.Get(testKey);
            var Newresult = _store.Get(newKeyName);
            Assert.IsNull(Oldresult);
            Assert.That(Newresult, Is.EqualTo(newValue));
        }
        [Test]
        public void RemoveOldKeys_WhenCalledWitCustomTime_RemovesNoKeys()
        {
            string date = _now.AddMonths(-5).ToString("yyyyMM");
            string oldValue = "oldTestValue";
            string newValue = "newTestValue";
            string newKeyName = "newTestKey";
            KeyValueStore storeDatetime = new KeyValueStore(path, new DeltaTime(months: 1), 6, OverwriteSetting: true);
            storeDatetime.Set(newKeyName, newValue);
            AddValueToJson(path, testKey + date, oldValue);
            storeDatetime = null; // reset store
            storeDatetime = new KeyValueStore(path);
            var Oldresult = storeDatetime.Get(testKey, 5);
            var Newresult = storeDatetime.Get(newKeyName);
            Assert.That(Oldresult, Is.EqualTo(oldValue));
            Assert.That(Newresult, Is.EqualTo(newValue));
        }
        private void CreateNewStore(string date, object value, string path, DeltaTime deltaTime)
        {

            var data = new Dictionary<string, object>
            {
                { testKey + date, value }
            };
            var newStore = new
            {
                Settings = new
                {
                    ContinuesStoreTime = deltaTime,
                    cleanUpTime = defaultCleanUpTime
                },
                Store = data
            };

            string json = JsonConvert.SerializeObject(newStore);
            File.WriteAllText(path, json);

            _store = new KeyValueStore(path);
        }
        private void AddValueToJson(string path, string key, string value)
        {
            if (File.Exists(path))
            {
                string json = File.ReadAllText(path);
                var store = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
                var jsonStore = JsonConvert.SerializeObject(store["Store"]);
                var data = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonStore);
                data[key] = value;
                var jsonSettings = JsonConvert.SerializeObject(store["Settings"]);
                var settings = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonSettings);
                 var newStore = new
                {
                    Settings = settings,
                    Store = data
                };
                json = JsonConvert.SerializeObject(newStore);
                File.WriteAllText(path, json);
            }
            else
            {
                var data = new Dictionary<string, object>
                {
                    { key, value }
                };
                var newStore = new
                {
                    Settings = new
                    {
                        ContinuesStoreTime = new DeltaTime(),
                        cleanUpTime = defaultCleanUpTime
                    },
                    Store = data
                };
                string json = JsonConvert.SerializeObject(newStore);
                File.WriteAllText(path, json);
            }
        }

    }
}
