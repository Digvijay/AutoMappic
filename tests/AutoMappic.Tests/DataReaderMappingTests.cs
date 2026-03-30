using System.Collections.Generic;
using System.Data;
using System.Linq;
using AutoMappic;
using Prova;

namespace AutoMappic.Tests;

public class DataReaderMappingTests
{
    public class Dest { public int Id { get; set; } public string Name { get; set; } = ""; }

    private class MockDataReader : IDataReader
    {
        public bool Read() => false;
        public void Dispose() { }
        public int FieldCount => 0;
        public bool IsDBNull(int i) => false;
        public string GetName(int i) => "";
        public object GetValue(int i) => null!;
        // ... many more ...
        public void Close() { }
        public int Depth => 0;
        public DataTable? GetSchemaTable() => null;
        public bool IsClosed => true;
        public int RecordsAffected => 0;
        public object this[int i] => null!;
        public object this[string name] => null!;
        public bool GetBoolean(int i) => false;
        public byte GetByte(int i) => 0;
        public long GetBytes(int i, long fieldOffset, byte[]? buffer, int bufferoffset, int length) => 0;
        public char GetChar(int i) => '\0';
        public long GetChars(int i, long fieldoffset, char[]? buffer, int bufferoffset, int length) => 0;
        public IDataReader GetData(int i) => null!;
        public string GetDataTypeName(int i) => "";
        public System.DateTime GetDateTime(int i) => System.DateTime.MinValue;
        public decimal GetDecimal(int i) => 0;
        public double GetDouble(int i) => 0;
        public System.Type GetFieldType(int i) => typeof(object);
        public float GetFloat(int i) => 0;
        public System.Guid GetGuid(int i) => System.Guid.Empty;
        public short GetInt16(int i) => 0;
        public int GetInt32(int i) => 0;
        public long GetInt64(int i) => 0;
        public string GetString(int i) => "";
        public int GetValues(object[] values) => 0;
        public int GetOrdinal(string name) => -1;
        public bool NextResult() => false;
    }

    public class DataReaderProfile : Profile
    {
        public DataReaderProfile() { CreateMap<IDataReader, Dest>(); }
    }

    [Fact]
    [Prova.Description("Verify that DataReader.Map works when a profile exists.")]
    public void DataReader_Map_ExecutesWithoutError()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<DataReaderProfile>();
        });

        using var reader = new MockDataReader();
        var results = reader.Map<Dest>().ToList();

        // This test primarily checks that the interception/compilation works.
        // At runtime, it will fall back to reflection if interception failed.
        // But we want to ensure interception can actually happen.
    }
}
