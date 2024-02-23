# AerensStore.KeyValuePairStore

AerensStore.KeyValuePairStore is a project within the AerensStores solution that provides a key-value store for storing data. The data is stored in a file and can be of any type that can be serialized to JSON.

## Classes

### `DeltaTime`

The `DeltaTime` class represents a time span with properties for years, months, days, and hours. This class is a support class for `KeyValueStore` where it determines the timedelta used for versioning of the keypairs. The `DeltaTime` class has the following properties:

- `Years`: Represents the number of years in the time span.
- `Months`: Represents the number of months in the time span.
- `Days`: Represents the number of days in the time span.
- `Hours`: Represents the number of hours in the time span.
- `IsOff`: A boolean value that indicates whether the time span is turned off. This property is set automatically and does not require user input.

### `KeyValueStore`

The `KeyValueStore` class provides a key-value store for storing data. It allows you to set and get values by key. The values are stored in a file and can be of any type that can be serialized to JSON. The `KeyValueStore` class has the following methods:

- `Set`: This method allows you to set a value for a given key. The value is serialized to JSON and stored in a file.
- `Get`: This method allows you to get the value for a given key. The value is deserialized from JSON.
- `RemoveOldKeys`: This method removes keys that are older than a specified `DeltaTime`.

The `KeyValueStore` constructor does not require any parameters. However, it does have optional parameters:

- `filePath`: If provided, this is where the key-value pairs and settings will be/are stored.
- `continuesStoreTime`: A `DeltaTime` object that determines when to remove old keys.
- `cleanUpTime`: An integer that specifies the time interval (in seconds) at which the store should be cleaned up.
- `OverwriteSetting`: A boolean that determines whether existing settings should be overwritten.

If these parameters are not provided, the `KeyValueStore` will use default values.

## Getting Started

TODO: write after nuget is published
## Contributing

Contributions to the KeyValuePairStore project are welcome. If you have a feature request, bug report, or want to contribute code, please open an issue or pull request on the GitHub repository.

## License

The KeyValuePairStore project is licensed under the terms of the license included in the `LICENSE.txt` file.