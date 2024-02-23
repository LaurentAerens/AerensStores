using System.Reflection;
using System.Runtime.InteropServices;
using System.Xml.XPath;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public class KeyValueStore
{
    private Dictionary<string, object> store;
    private string filePath;
    DeltaTime ContinuesStoreTime = new DeltaTime();
    int cleanUpTime;


    /// <summary>
    /// Represents a key-value store that persists data to a file.
    /// </summary>
    /// <param name="filePath">The path to the file where the data is stored. If null, a default file path will be used.</param>
    /// <param name="continuesStoreTime">The delta time for continuous storage. If null there will be no date based storage and no cleanup</param>
    /// <param name="cleanUpTime">The clean-up time in x * set DeltaTimes. If 0 and continuous storage is set to 3 months, a default clean-up time of 3 months will be used. So anything older then 5 months will be deleted</param>
    public KeyValueStore(string filePath = null, DeltaTime continuesStoreTime = null, int cleanUpTime = 0, bool OverwriteSetting = false)
    {
        bool MissmatchSettings = false;
        if (continuesStoreTime == null)
        {
            continuesStoreTime = new DeltaTime();
        }
        ContinuesStoreTime = continuesStoreTime;
        if (!ContinuesStoreTime.IsOff && cleanUpTime == 0)
        {
            cleanUpTime = 3;
        }
        this.cleanUpTime = cleanUpTime;
        if (filePath == null)
        {
            filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "keyValueStore.json");
        }
        this.filePath = filePath;

        if (File.Exists(filePath))
        {
            var content = File.ReadAllText(filePath);
            var data = JsonConvert.DeserializeObject<Dictionary<string, object>>(content);
            var settingsjson = JsonConvert.SerializeObject(data["Settings"]);
            var settings = JsonConvert.DeserializeObject<Dictionary<string, object>>(settingsjson);
            var storejson = JsonConvert.SerializeObject(data["Store"]);
            store = JsonConvert.DeserializeObject<Dictionary<string, object>>(storejson);
            if (!OverwriteSetting) 
            {
                if (settings.ContainsKey("ContinuesStoreTime") && settings.ContainsKey("cleanUpTime"))
                {
                    string JsonContinuesStoreTime = JsonConvert.SerializeObject(settings["ContinuesStoreTime"]);
                    DeltaTime ContinuesStoreTimeSettings = CreateDeltaTimeFromJson(JsonContinuesStoreTime);

                    if (ContinuesStoreTime.IsOff)
                    {
                        ContinuesStoreTime = ContinuesStoreTimeSettings;
                    }
                    else
                    {
                        if (ContinuesStoreTime != ContinuesStoreTimeSettings)
                        {
                            MissmatchSettings = true;
                        }
                    }
                    if (cleanUpTime == 0)
                    {
                        cleanUpTime = (int)(long)settings["cleanUpTime"];
                    }
                    else
                    {
                        if (cleanUpTime != (int)(long)settings["cleanUpTime"])
                        {
                            MissmatchSettings = true;
                        }
                    }
                }
                else
                {
                    MissmatchSettings = true;
                }
            }
        }
        else
        {
            store = new Dictionary<string, object>();
        }
        Save();
        AppDomain.CurrentDomain.ProcessExit += (s, e) => RemoveOldKeys();
        if (MissmatchSettings)
        {
            throw new ArgumentException("The settings in the keyValueStore en the contructor do not match, Constructor onces are taken: This might brake stuff.");
        }
    }
    public DeltaTime CreateDeltaTimeFromJson(string json)
    {
        var jObject = JObject.Parse(json);

        int years = (int)jObject["Years"];
        int months = (int)jObject["Months"];
        int days = (int)jObject["Days"];
        int hours = (int)jObject["Hours"];

        return new DeltaTime(years, months, days, hours);
    }
    private void Save()
    {
        var settings = new Dictionary<string, object>
        {
            { "ContinuesStoreTime", ContinuesStoreTime },
            { "cleanUpTime", cleanUpTime }
        };

        var data = new Dictionary<string, object>
        {
            { "Settings", settings },
            { "Store", store }
        };

        var content = JsonConvert.SerializeObject(data);
        File.WriteAllText(filePath, content);
    }

    /// <summary>
    /// Sets the value associated with the specified key in the key-value store.
    /// If ContinuesStoreTime is off, the value is directly stored in the store and saved.
    /// If ContinuesStoreTime is on, We update the key or make a new one if that is needed.
    /// </summary>
    /// <param name="key">The key to associate the value with.</param>
    /// <param name="value">The value to be stored.</param>
    public void Set(string key, object value)
    {
        if (ContinuesStoreTime.IsOff)
        {
            store[key] = value;
            Save();
            return;
        }
        else
        {
            List<string> keyNames = Checkkeys(key);
            foreach (string keyName in keyNames)
            {
                if (store.ContainsKey(keyName))
                {
                    store[keyName] = value;
                }
            }
            store[GenereateKey(key)] = value;
            Save();
        }
    }

    /// <summary>
    /// Retrieves the value associated with the specified key.
    /// </summary>
    /// <param name="key">The key of the value to retrieve.</param>
    /// <returns>The value associated with the specified key, or null if the key is not found.</returns>
    public object Get(string key, int iterationsAgo = 0)
    {
        try
        {
            object result = null;
            if (ContinuesStoreTime.IsOff)
            {
                return store[key];
            }
            else
            {
                List<string> keyNames = Checkkeys(key, iterationsAgo);
                foreach (string keyName in keyNames)
                {
                    if (store.ContainsKey(keyName))
                    {
                        result = store[keyName];
                    }
                }
            }
            return result;
        }
        catch (KeyNotFoundException)
        {
            return null;
        }

    }

    /// <summary>
    /// Sets the value of a string key in the key-value store.
    /// </summary>
    /// <param name="key">The key to set.</param>
    /// <param name="value">The value to set.</param>
    public void SetString(string key, string value)
    {
        Set(key, value);
    }
    /// <summary>
    /// Retrieves the value associated with the specified key as a string.
    /// </summary>
    /// <param name="key">The key of the value to retrieve.</param>
    /// <param name="iterationsAgo">
    /// The number of iterations ago to get the value from. This is multiplied by DeltaTime to calculate the actual time ago.
    /// If the value from the specified number of iterations ago does not exist, an error is thrown instead of returning empty string.
    /// </param>
    /// <returns>The value associated with the specified key as a string, or empty string if the key does not exist or the value is not a string.</returns>
    /// <exception cref="KeyNotFoundException">Thrown when the key does not exist in the store and iterationsAgo != 0.</exception>
    /// <exception cref="InvalidCastException">Thrown when the value associated with the key is not a string.</exception>
    public string GetString(string key, int iterationsAgo = 0)
    {
        var value = Get(key, iterationsAgo);
        if (value == null)
        {
            return string.Empty;
        }
        else if (value is string stringValue)
        {
            return stringValue;
        }
        else
        {
            throw new InvalidCastException($"The value for key '{key}' is not of type 'string'.");
        }
    }

    /// <summary>
    /// Sets the value of the specified key as an integer.
    /// </summary>
    /// <param name="key">The key to set.</param>
    /// <param name="value">The integer value to set.</param>
    public void SetInt(string key, int value)
    {
        Set(key, value);
    }

    /// <summary>
    /// Retrieves the value associated with the specified key as a integer.
    /// </summary>
    /// <param name="key">The key of the value to retrieve.</param>
    /// <param name="iterationsAgo">
    /// The number of iterations ago to get the value from. This is multiplied by DeltaTime to calculate the actual time ago.
    /// If the value from the specified number of iterations ago does not exist, an error is thrown instead of returning 0.
    /// </param>
    /// <returns>The value associated with the specified key as a integer, or 0 if the key does not exist or the value is not a integer.</returns>
    /// <exception cref="KeyNotFoundException">Thrown when the key does not exist in the store and iterationsAgo != 0.</exception>
    /// <exception cref="InvalidCastException">Thrown when the value associated with the key is not a integer.</exception>
    public int GetInt(string key, int iterationsAgo = 0)
    {
        var value = Get(key, iterationsAgo);
        if (value == null)
        {
            return 0;
        }
        else if (value is int intValue)
        {
            return intValue;
        }
        else
        {
            try
            {
                return Convert.ToInt32(value);
            }
            catch (InvalidCastException)
            {
                throw new InvalidCastException($"The value for key '{key}' is not of type 'int'.");
            }
            catch (FormatException)
            {
                throw new InvalidCastException($"The value for key '{key}' is not of type 'int'."); 
            }
            
        }
    }

    /// <summary>
    /// Sets the value of the specified key as a double.
    /// </summary>
    /// <param name="key">The key to set.</param>
    /// <param name="value">The double value to set.</param>
    public void SetDouble(string key, double value)
    {
        Set(key, value);
    }

    /// <summary>
    /// Retrieves the value associated with the specified key as a double.
    /// </summary>
    /// <param name="key">The key of the value to retrieve.</param>
    /// <param name="iterationsAgo">
    /// The number of iterations ago to get the value from. This is multiplied by DeltaTime to calculate the actual time ago.
    /// If the value from the specified number of iterations ago does not exist, an error is thrown instead of returning 0.
    /// </param>
    /// <returns>The value associated with the specified key as a double, or 0 if the key does not exist or the value is not a double.</returns>
    /// <exception cref="KeyNotFoundException">Thrown when the key does not exist in the store and iterationsAgo != 0.</exception>
    /// <exception cref="InvalidCastException">Thrown when the value associated with the key is not a double.</exception>
    public double GetDouble(string key, int iterationsAgo = 0)
    {
        var value = Get(key, iterationsAgo);
        if (value == null)
        {
            return 0;
        }
        else if (value is double doubleValue)
        {
            return doubleValue;
        }
        else
        {
            throw new InvalidCastException($"The value for key '{key}' is not of type 'double'.");
        }
    }

    /// <summary>
    /// Sets the value of the specified key as a long.
    /// </summary>
    /// <param name="key">The key to set.</param>
    /// <param name="value">The long value to set.</param>
    public void SetLong(string key, long value)
    {
        Set(key, value);
    }

    /// <summary>
    /// Retrieves the value associated with the specified key as a long.
    /// </summary>
    /// <param name="key">The key of the value to retrieve.</param>
    /// <param name="iterationsAgo">
    /// The number of iterations ago to get the value from. This is multiplied by DeltaTime to calculate the actual time ago.
    /// If the value from the specified number of iterations ago does not exist, an error is thrown instead of returning 0.
    /// </param>
    /// <returns>The value associated with the specified key as a long, or 0 if the key does not exist or the value is not a long.</returns>
    /// <exception cref="KeyNotFoundException">Thrown when the key does not exist in the store and iterationsAgo != 0.</exception>
    /// <exception cref="InvalidCastException">Thrown when the value associated with the key is not a long.</exception>
    public long GetLong(string key, int iterationsAgo = 0)
    {
        var value = Get(key, iterationsAgo);
        if (value == null)
        {
            return 0;
        }
        else if (value is long longValue)
        {
            return longValue;
        }
        else
        {
            throw new InvalidCastException($"The value for key '{key}' is not of type 'long'.");
        }
    }

    /// <summary>
    /// Sets the value of the specified key as a char.
    /// </summary>
    /// <param name="key">The key to set.</param>
    /// <param name="value">The char value to set.</param>
    public void SetChar(string key, char value)
    {
        Set(key, value);
    }

    /// <summary>
    /// Retrieves the value associated with the specified key as a char.
    /// </summary>
    /// <param name="key">The key of the value to retrieve.</param>
    /// <param name="iterationsAgo">
    /// The number of iterations ago to get the value from. This is multiplied by DeltaTime to calculate the actual time ago.
    /// If the value from the specified number of iterations ago does not exist, an error is thrown instead of returning '\0'.
    /// </param>
    /// <returns>The value associated with the specified key as a char, or '\0' if the key does not exist or the value is not a char.</returns>
    /// <exception cref="KeyNotFoundException">Thrown when the key does not exist in the store and iterationsAgo != 0.</exception>
    /// <exception cref="InvalidCastException">Thrown when the value associated with the key is not a char.</exception>
    public char GetChar(string key, int iterationsAgo = 0)
    {
        var value = Get(key, iterationsAgo);
        if (value == null)
        {
            return '\0';
        }
        else if (value is char charValue)
        {
            return charValue;
        }
        else
        {
            try
            {
                char charvalue = Convert.ToChar(value);
                if (charvalue == '\0')
                {
                    throw new InvalidCastException($"The value for key '{key}' is not of type 'char'.");
                }
                else
                {
                    return charvalue;
                }
            }
            catch (InvalidCastException)
            {
                throw new InvalidCastException($"The value for key '{key}' is not of type 'char'.");
            }
        }
    }

    /// <summary>
    /// Sets the value of the specified key as a bool.
    /// </summary>
    /// <param name="key">The key to set.</param>
    /// <param name="value">The bool value to set.</param>
    public void SetBool(string key, bool value)
    {
        Set(key, value);
    }

    /// <summary>
    /// Retrieves the value associated with the specified key as a bool.
    /// </summary>
    /// <param name="key">The key of the value to retrieve.</param>
    /// <param name="iterationsAgo">
    /// The number of iterations ago to get the value from. This is multiplied by DeltaTime to calculate the actual time ago.
    /// If the value from the specified number of iterations ago does not exist, an error is thrown instead of returning false.
    /// </param>
    /// <returns>The value associated with the specified key as a bool, or false if the key does not exist or the value is not a bool.</returns>
    /// <exception cref="KeyNotFoundException">Thrown when the key does not exist in the store and iterationsAgo != 0.</exception>
    /// <exception cref="InvalidCastException">Thrown when the value associated with the key is not a bool.</exception>
    public bool GetBool(string key, int iterationsAgo = 0)
    {
        var value = Get(key, iterationsAgo);
        if (value == null)
        {
            return false;
        }
        else if (value is bool boolValue)
        {
            return boolValue;
        }
        else
        {
            throw new InvalidCastException($"The value for key '{key}' is not of type 'bool'.");
        }
    }


    private List<string> Checkkeys(string key, int iterationsAgo = 0)
    {
        int[] timeToKeep = ContinuesStoreTime.GetTimeValues();
        List<string> keyNames = new List<string>();


        if (timeToKeep[0] != 0)
        {
            for (int i = 0; i < timeToKeep[0]; i++)
            {
                keyNames.Add(key + DateTime.Now.AddHours(-i - iterationsAgo * timeToKeep[0]).ToString("yyyyMMDDHH"));
            }
        }
        else if (timeToKeep[1] != 0)
        {
            for (int i = 0; i < timeToKeep[1]; i++)
            {
                keyNames.Add(key + DateTime.Now.AddDays(-i - iterationsAgo * timeToKeep[1]).ToString("yyyyMMDD"));
            }
        }
        else if (timeToKeep[2] != 0)
        {
            for (int i = 0; i < timeToKeep[2]; i++)
            {
                keyNames.Add(key + DateTime.Now.AddMonths(-i - iterationsAgo * timeToKeep[2]).ToString("yyyyMM"));
            }
        }
        else if (timeToKeep[3] != 0)
        {
            for (int i = 0; i < timeToKeep[3]; i++)
            {
                keyNames.Add(key + DateTime.Now.AddYears(-i - iterationsAgo * timeToKeep[3]).ToString("yyyy"));
            }
        }
        else
        {
            keyNames.Add(key);
        }

        return keyNames;
    }
    private string GenereateKey(string key)
    {

        int[] timeToKeep = ContinuesStoreTime.GetTimeValues();
        string keyName;

        if (timeToKeep[0] != 0)
        {
            keyName = key + DateTime.Now.ToString("yyyyMMDDHH");
        }
        else if (timeToKeep[1] != 0)
        {
            keyName = key + DateTime.Now.ToString("yyyyMMDD");
        }
        else if (timeToKeep[2] != 0)
        {
            keyName = key + DateTime.Now.ToString("yyyyMM");
        }
        else if (timeToKeep[3] != 0)
        {
            keyName = key + DateTime.Now.ToString("yyyy");
        }
        else
        {
            keyName = key;
        }

        return keyName;
    }
    // cleanup stuff
    private void RemoveOldKeys()
    {
        var (minLenght, minDate, TimeMode) = CalculateCleanupData();
        if (TimeMode == 4)
        {
            return;
        }
        var currentDate = DateTime.Now;
        var keysToRemove = new List<string>();
        foreach (var key in store.Keys)
        {
            if (IsOldKey(key, currentDate, minLenght, minDate, TimeMode))
            {
                keysToRemove.Add(key);
            }
        }

        foreach (var key in keysToRemove)
        {
            store.Remove(key);
        }

        Save();
    }

    private bool IsOldKey(string key, DateTime currentDate, int minLenght, DateTime MinDate, int TimeMode)
    {

        if (key.Length < minLenght)
        {
            return false;
        }

        string Datestring = key.Substring(key.Length - minLenght);
        int year, month, day, hour;
        DateTime keyDate = new DateTime();
        switch (TimeMode)
        {
            case 0:
                if (!int.TryParse(Datestring.Substring(0, 4), out year) || !int.TryParse(Datestring.Substring(4, 2), out month) || !int.TryParse(key.Substring(6, 2), out day) || !int.TryParse(key.Substring(8, 2), out hour))
                {
                    return false;
                }
                keyDate = new DateTime(year, month, day, hour, 0, 0);
                return keyDate < MinDate;
            case 1:
                if (!int.TryParse(Datestring.Substring(0, 4), out year) || !int.TryParse(Datestring.Substring(4, 2), out month) || !int.TryParse(key.Substring(6, 2), out day))
                {
                    return false;
                }
                keyDate = new DateTime(year, month, day);
                return keyDate < MinDate;
            case 2:
                if (!int.TryParse(Datestring.Substring(0, 4), out year) || !int.TryParse(Datestring.Substring(4, 2), out month))
                {
                    return false;
                }
                keyDate = new DateTime(year, month, 1);
                return keyDate < MinDate;
            case 3:
                if (!int.TryParse(Datestring.Substring(0, 4), out year))
                {
                    return false;
                }
                keyDate = new DateTime(year, 1, 1);
                return keyDate < MinDate;
            default:
                return false;
        }
    }
    private (int, DateTime, int) CalculateCleanupData()
    {
        if (ContinuesStoreTime.IsOff)
        {
            return (0, DateTime.Now, 4);
        }
        int mode = 0;
        DateTime minDate = DateTime.Now;
        int minLenght = 0;

        int[] timeToKeep = ContinuesStoreTime.GetTimeValues();
        if (timeToKeep[0] != 0)
        {
            mode = 0;
            minLenght = 10;
            minDate = DateTime.Now.AddHours(-timeToKeep[0] - cleanUpTime);
        }
        else if (timeToKeep[1] != 0)
        {
            mode = 1;
            minLenght = 8;
            minDate = DateTime.Now.AddDays(-timeToKeep[1] - cleanUpTime);
        }
        else if (timeToKeep[2] != 0)
        {
            mode = 2;
            minLenght = 6;
            minDate = DateTime.Now.AddMonths(-timeToKeep[2] - cleanUpTime);
        }
        else if (timeToKeep[3] != 0)
        {
            mode = 3;
            minLenght = 4;
            minDate = DateTime.Now.AddYears(-timeToKeep[3] - cleanUpTime);
        }
        else
        {
            mode = 4;
            minLenght = 0;
            minDate = DateTime.Now;
        }
        return (minLenght, minDate, mode);

    }


}
