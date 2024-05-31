# AerensStores

AerensStores is a collection of projects that provide various utilities for data storage and manipulation. The projects in this solution are still under heavy development and once they are stable they will be moved to their own repositories.
The solution currently includes the following projects:

## KeyValuePairStore

This project provides a key-value store for storing data. It includes the `KeyValueStore` class which allows you to set and get values by key. The values are stored in a file and can be of any type that can be serialized to JSON.

The `DeltaTime` class is also part of this project. It represents a time span with properties for years, months, days, and hours. This class is used in the `KeyValueStore` class to determine when to remove old keys.

This Project has been graduated to it's own repository [here](https://github.com/LaurentAerens/KeyValuePairStore). 

## AerensStoreUnittest

This project contains unit tests for the `KeyValuePairStore` project. It includes tests for both the `KeyValueStore` and `DeltaTime` classes. The tests ensure that the key-value store correctly sets and gets values, and that it correctly removes old keys based on the `DeltaTime`.

The tests use the NUnit testing framework and Moq for mocking.

## Future Projects

More projects will be added to the AerensStores solution in the future. These projects will provide additional utilities and will be tested in the `AerensStoreUnittest` project.

## Getting Started

To get started with AerensStores, clone the repository and open the `AerensStores.sln` file in Visual Studio. You can run the `KeyValuePairStore` project to see the key-value store in action, and you can run the tests in the `AerensStoreUnittest` project to verify that everything is working correctly.

## Contributing

Contributions to AerensStores are welcome. If you have a feature request, bug report, or want to contribute code, please open an issue or pull request on the GitHub repository.

## License

AerensStores is licensed under the terms of the license included in the `LICENSE.txt` file.# AerensStores