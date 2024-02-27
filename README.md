This package is a C# wrapper around [delta-rs](https://github.com/delta-io/delta-rs/tree/rust-v0.17.0).

## Known Issues

The [Arrow Library for C#](https://github.com/apache/arrow/blob/main/csharp/README.md) does not support large types. This means tables with these types will not work with this library. The user will see a `NotImplementException`.
