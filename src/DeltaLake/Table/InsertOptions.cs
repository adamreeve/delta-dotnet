using System.Collections;
using System.Collections.Generic;
using Apache.Arrow;

namespace DeltaLake.Table
{
    /// <summary>
    /// Options for inserting data into a table
    /// </summary>
    public class InsertOptions
    {
        /// <summary>
        /// Creates an instance of InsertOptions with required Records property
        /// </summary>
        /// <param name="records"><see cref="RecordBatch"/> The records to insert</param>
        public InsertOptions(IReadOnlyCollection<RecordBatch> records)
        {
            Records = records;
        }

        /// <summary>
        /// <see cref="Apache.Arrow.RecordBatch">RecordBatch</see> representing records to insert
        /// </summary>
        public IReadOnlyCollection<RecordBatch> Records { get; }

        /// <summary>
        /// Predicate for insertion.
        /// Represents the WHERE clause, not including WHERE
        /// </summary>
        public string? Predicate { get; init; }

        /// <summary>
        /// <see cref="SaveMode" />
        /// </summary>
        public SaveMode SaveMode { get; init; }

        /// <summary>
        /// Maximum number of rows to write per row group
        /// </summary>
        public ulong MaxRowsPerGroup { get; init; } = 100;

        /// <summary>
        /// Overwrite schema with schema from record batch
        /// </summary>
        public bool OverwriteSchema { get; init; }
    }
}