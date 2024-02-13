using Apache.Arrow;
using Apache.Arrow.Memory;
using Apache.Arrow.Types;
using DeltaLake.Runtime;
using DeltaLake.Table;

namespace DeltaLake.Tests.Table;

public class DeltaTableTests
{
    [Fact]
    public async Task Create_InMemory_Test()
    {
        var uri = $"memory://{Guid.NewGuid():N}";
        using var runtime = new DeltaRuntime(RuntimeOptions.Default);
        var builder = new Apache.Arrow.Schema.Builder();
        builder.Field(fb =>
        {
            fb.Name("test");
            fb.DataType(Int32Type.Default);
            fb.Nullable(false);
        });
        var schema = builder.Build();
        using var table = await DeltaTable.CreateAsync(
            runtime,
            new TableCreateOptions(uri, schema)
            {
                Configuration = new Dictionary<string, string?>
                {
                    ["delta.dataSkippingNumIndexedCols"] = "32",
                    ["delta.setTransactionRetentionDuration"] = null,
                }
            },
            CancellationToken.None);
        Assert.NotNull(table);
        var version = table.Version();
        Assert.Equal(0, version);
        var location = table.Location();
        Assert.Equal(uri, location);
        var files = table.Files();
        Assert.Empty(files);
        var fileUris = table.FileUris();
        Assert.Empty(fileUris);
        var returnedSchema = table.Schema();
        Assert.NotNull(returnedSchema);
        Assert.Equal(schema.FieldsList.Count, returnedSchema.FieldsList.Count);
    }

    [Fact]
    public async Task Create_InMemory_With_Partitions_Test()
    {
        var uri = $"memory://{Guid.NewGuid():N}";
        using var runtime = new DeltaRuntime(RuntimeOptions.Default);
        var builder = new Apache.Arrow.Schema.Builder();
        builder.Field(fb =>
        {
            fb.Name("test");
            fb.DataType(Int32Type.Default);
            fb.Nullable(false);
        })
        .Field(fb =>
        {
            fb.Name("second");
            fb.DataType(Int32Type.Default);
            fb.Nullable(false);
        });
        var schema = builder.Build();
        var createOptions = new TableCreateOptions(uri, schema)
        {
            Configuration = new Dictionary<string, string?>
            {
                ["delta.dataSkippingNumIndexedCols"] = "32",
                ["delta.setTransactionRetentionDuration"] = null,
            },
            PartitionBy = { "test" },
            Name = "table",
            Description = "this table has a description",
            CustomMetadata = new Dictionary<string, string> { ["test"] = "something" },
            StorageOptions = new Dictionary<string, string> { ["something"] = "here" },
        };
        using var table = await DeltaTable.CreateAsync(
            runtime,
            createOptions,
            CancellationToken.None);
        Assert.NotNull(table);
        var version = table.Version();
        Assert.Equal(0, version);
        var location = table.Location();
        Assert.Equal(uri, location);
        var metadata = table.Metadata();
        Assert.Single(metadata.PartitionColumns);
        Assert.Equal("test", metadata.PartitionColumns[0]);
        Assert.Equal(createOptions.Name, metadata.Name);
        Assert.Equal(createOptions.Description, metadata.Description);
    }

    [Fact]
    public async Task Load_Table_Test()
    {
        var location = Path.Join(Settings.TestRoot, "simple_table");
        using var runtime = new DeltaRuntime(RuntimeOptions.Default);
        using var table = await DeltaTable.LoadAsync(runtime, location, new TableOptions(),
        CancellationToken.None);
        Assert.Equal(4, table.Version());
    }

    [Fact]
    public async Task Load_Table_Memory_Test()
    {
        var location = Path.Join(Settings.TestRoot, "simple_table");
        var memory = System.Text.Encoding.UTF8.GetBytes(location);
        using var runtime = new DeltaRuntime(RuntimeOptions.Default);
        using var table = await DeltaTable.LoadAsync(runtime, memory.AsMemory(), new TableOptions(),
        CancellationToken.None);
        Assert.Equal(4, table.Version());
    }

    [Fact]
    public async Task Table_Insert_Test()
    {
        var uri = $"memory://{Guid.NewGuid():N}";
        using var runtime = new DeltaRuntime(RuntimeOptions.Default);
        var builder = new Apache.Arrow.Schema.Builder();
        builder.Field(fb =>
        {
            fb.Name("test");
            fb.DataType(Int32Type.Default);
            fb.Nullable(false);
        });
        var schema = builder.Build();
        using var table = await DeltaTable.CreateAsync(
            runtime,
            new TableCreateOptions(uri, schema),
            CancellationToken.None);
        Assert.NotNull(table);
        int length = 10;
        var allocator = new NativeMemoryAllocator();
        var recordBatchBuilder = new RecordBatch.Builder(allocator)
            .Append("test", false, col => col.Int32(arr => arr.AppendRange(Enumerable.Range(0, length))));


        var options = new InsertOptions
        {
            SaveMode = SaveMode.Append,
        };
        await table.InsertAsync([recordBatchBuilder.Build()], schema, options, CancellationToken.None);
        var version = table.Version();
        var queryResult = table.QueryAsync(new SelectQuery("SELECT test FROM test WHERE test > 1")
        {
            TableAlias = "test",
        },
        CancellationToken.None).ToBlockingEnumerable().ToList();
        Assert.Equal(1, version);
        var resultCount = 0;
        foreach (var batch in queryResult)
        {
            Assert.Equal(1, batch.ColumnCount);
            var column = batch.Column(0);
            if (column is not Int32Array integers)
            {
                throw new Exception("expected int32 array and got " + column.GetType());
            }

            foreach (var intValue in integers)
            {
                Assert.NotNull(intValue);
                Assert.True(intValue.Value > 1);
                ++resultCount;
            }
        }

        Assert.Equal(8, resultCount);
        await foreach (var result in table.QueryAsync(new SelectQuery("SELECT test FROM test WHERE test = 1")
        {
            TableAlias = "test",
        },
        CancellationToken.None))
        {
            Assert.NotNull(result);
        }
        var history = await table.HistoryAsync(1, CancellationToken.None);
        Assert.Single(history);
        history = await table.HistoryAsync(default, CancellationToken.None);
        Assert.Equal(2, history.Length);
    }
}