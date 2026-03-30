using System.Threading.Tasks;
using AutoMappic;
using Prova;

namespace AutoMappic.Tests
{
    public class IgnoreAttributeTests
    {
        public class Source { public string Name { get; set; } = "Alice"; public string Secret { get; set; } = "Pass"; }
        public class Dest
        {
            public string Name { get; set; } = "";
            [AutoMappicIgnore]
            public string Secret { get; set; } = "Old";
        }

        public class IgnoreProfile : Profile
        {
            public IgnoreProfile() => CreateMap<Source, Dest>();
        }

        [Fact]
        public void Map_WithIgnoreAttribute_ShouldNotMapProperty()
        {
            // Arrange
            var source = new Source();
            var mapper = new MapperConfiguration(cfg => cfg.AddProfile<IgnoreProfile>()).CreateMapper();

            // Act
            var result = mapper.Map<Dest>(source);

            // Assert
            Assert.Equal("Alice", result.Name);
            Assert.Equal("Old", result.Secret);
        }

        public class UnmappedDest
        {
            [AutoMappicIgnore]
            public string RequiredButIgnored { get; set; } = "";
            [AutoMappicIgnore]
            public string IgnoredSecret { get; set; } = "";
        }

        public class UnmappedIgnoreProfile : Profile
        {
            public UnmappedIgnoreProfile() => CreateMap<object, UnmappedDest>();
        }

        [Fact]
        public void Map_WithRequiredButIgnored_ShouldNotThrowErrorIfIgnored()
        {
            var config = new MapperConfiguration(cfg => cfg.AddProfile<UnmappedIgnoreProfile>());
            var mapper = config.CreateMapper();
            var source = new object();
            var result = mapper.Map<UnmappedDest>(source);
            Assert.Equal("", result.RequiredButIgnored);
        }
    }
}
