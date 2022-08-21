using codecrafters_sqlite.Query;
using codecrafters_sqlite.Utils;
using static System.Buffers.Binary.BinaryPrimitives;


namespace codecrafters_sqlite.DB;


public class RecordHelper
{
    public static Record[] GetRecordsByTablePage(
        string path, 
        int pageSize, 
        int pageNumber, 
        Dictionary<Column, string> conditions)
    {
        var pageBytes = PageHelper.ReadPage(path, pageSize, pageNumber);
        var startOfHeader = 0;
        var endOfHeader = 8;

        if (pageNumber == 1)
        {
            startOfHeader = 100;
            endOfHeader = 108;
        }

        // Parse page header from database
        var pageHeader = PageHeader.Parse(pageBytes[startOfHeader..endOfHeader]);
        if (pageHeader.PageType is BTreePage.InteriorTable or BTreePage.InteriorIndex)
        { 
            endOfHeader += 4;
        }
        
        // Obtain all cell pointers
        var cellPointers = pageBytes[endOfHeader..]
            .Chunk(2)
            .Take(pageHeader.NumberOfCells)
            .Select(bytes => ReadUInt16BigEndian(bytes));
        
        if (pageHeader.PageType == BTreePage.LeafTable)
        {
            var records = cellPointers
                .Select(cellPointer =>
                {
                    var stream = pageBytes[cellPointer..];

                    // bytesRead is the length of varint
                    var (payloadSize, bytesRead1) = Varint.Parse(stream);
                    var (rowId, bytesRead2) = Varint.Parse(stream[bytesRead1..]);

                    return Record.Parse(stream[(bytesRead1 + bytesRead2)..], rowId);
                })
                .Where(rec => checkRecord(rec, conditions))
                .ToArray();
            return records;
        }
        
        if (pageHeader.PageType == BTreePage.InteriorTable)
        {
            var records = cellPointers
                .SelectMany(cellPointer =>
                {
                    var stream = pageBytes[cellPointer..];

                    var leftChildPageNumber = ReadUInt32BigEndian(stream[..4]);
                    var (_rowId, bytesRead2) = Varint.Parse(stream[4..]);

                    return GetRecordsByTablePage(path, pageSize, (int)leftChildPageNumber, conditions);
                })
                .ToArray();
            return records;
        }

        throw new NotSupportedException();
    }
    
    public static Record[] GetRecordsByIndexPage(
        string path, 
        int pageSize, 
        int pageNumber, 
        string indexColValue,
        Dictionary<Column, string> conditions,
        int tableRootPageNumber)
    {
        var pageBytes = PageHelper.ReadPage(path, pageSize, pageNumber);
        var startOfHeader = 0;
        var endOfHeader = 8;

        if (pageNumber == 1)
        {
            startOfHeader = 100;
            endOfHeader = 108;
        }

        // Parse page header from database
        var pageHeader = PageHeader.Parse(pageBytes[startOfHeader..endOfHeader]);
        if (pageHeader.PageType is BTreePage.InteriorTable or BTreePage.InteriorIndex)
        { 
            endOfHeader += 4;
        }
        
        // Obtain all cell pointers
        var cellPointers = pageBytes[endOfHeader..]
            .Chunk(2)
            .Take(pageHeader.NumberOfCells)
            .Select(bytes => ReadUInt16BigEndian(bytes));

        if (pageHeader.PageType == BTreePage.LeafIndex)
        {
            var records = cellPointers
                .Select(cellPointer =>
                {
                    var stream = pageBytes[cellPointer..];

                    // bytesRead is the length of varint
                    var (payloadSize, bytesRead1) = Varint.Parse(stream);

                    var record = Record.Parse(stream[bytesRead1..], -1);
                    
                    if (record.Columns[0] == indexColValue)
                    {
                        record = GetRecordByRowId(path, pageSize, 
                            (uint)tableRootPageNumber, long.Parse(record.Columns[1]));
                    }
                    else
                    {
                        record = null;
                    }

                    return record;
                })
                .Where(rec => rec != null)
                .ToArray();
            return records;
        }
        
        if (pageHeader.PageType == BTreePage.InteriorIndex)
        {
            var records = cellPointers
                .SelectMany(cellPointer =>
                {
                    var stream = pageBytes[cellPointer..];

                    var leftChildPageNumber = ReadUInt32BigEndian(stream[..4]);
                    var (payloadSize, bytesRead1) = Varint.Parse(stream[4..]);
                    
                    var record = Record.Parse(stream[(4 + bytesRead1)..], -1);
                    
                    if (string.Compare(record.Columns[0], 
                            indexColValue, StringComparison.Ordinal) > 0)
                    {
                        return GetRecordsByIndexPage(path, pageSize, (int)leftChildPageNumber, 
                            indexColValue, conditions, tableRootPageNumber);
                    }

                    return Array.Empty<Record>();
                })
                .ToArray();
            return records;
        }

        throw new NotSupportedException();
    }

    public static Record GetRecordByRowId(
        string path,
        int pageSize,
        uint pageNumber,
        long rowId)
    {
        var pageBytes = PageHelper.ReadPage(path, pageSize, pageNumber);
        var startOfHeader = 0;
        var endOfHeader = 8;
        long rightMost = 0;

        if (pageNumber == 1)
        {
            startOfHeader = 100;
            endOfHeader = 108;
        }

        // Parse page header from database
        var pageHeader = PageHeader.Parse(pageBytes[startOfHeader..endOfHeader]);
        if (pageHeader.PageType is BTreePage.InteriorTable or BTreePage.InteriorIndex)
        { 
            endOfHeader += 4;
            rightMost = ReadUInt32BigEndian(pageBytes[8..12]);
        }
        
        // Obtain all cell pointers
        var cellPointers = pageBytes[endOfHeader..]
            .Chunk(2)
            .Take(pageHeader.NumberOfCells)
            .Select(bytes => ReadUInt16BigEndian(bytes))
            .ToArray();

        if (pageHeader.PageType == BTreePage.InteriorTable)
        {
            foreach (var cellPointer in cellPointers)
            {
                var stream = pageBytes[cellPointer..];

                var leftChildPageNumber = ReadUInt32BigEndian(stream[..4]);
                var (_rowId, bytesRead2) = Varint.Parse(stream[4..]);

                if (_rowId >= rowId)
                {
                    return GetRecordByRowId(path, pageSize, leftChildPageNumber, rowId);
                }
            }
            return GetRecordByRowId(path, pageSize, (uint)rightMost, rowId);
        }

        if (pageHeader.PageType == BTreePage.LeafTable)
        {
            foreach (var cellPointer in cellPointers)
            {
                var stream = pageBytes[cellPointer..];

                // bytesRead is the length of varint
                var (payloadSize, bytesRead1) = Varint.Parse(stream);
                var (_rowId, bytesRead2) = Varint.Parse(stream[bytesRead1..]);

                if (_rowId == rowId)
                {
                    return Record.Parse(stream[(bytesRead1 + bytesRead2)..], rowId);
                }
            }
        }
        
        throw new NotSupportedException();
    }

    private static bool checkRecord(
        Record record, 
        Dictionary<Column, string> conditions)
    {
        return conditions.All(cond =>
        {
            if (cond.Key.IsRowId())
            {
                return record.RowId.ToString() == cond.Value;
            }
            return record.Columns[cond.Key.Index] == cond.Value;
        });
    }
}