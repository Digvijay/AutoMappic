using System.Threading.Tasks;
using AutoMappic;
using Prova;

namespace AutoMappic.Tests
{
    public class MultiSourceTests
    {
        public record User(string Name, int Age);
        public record Preferences(string Theme, bool NotificationsEnabled);
        public record ProfileDto(string Name, int Age, string Theme, bool NotificationsEnabled);

        public class MultiSourceProfile : Profile
        {
            public MultiSourceProfile()
            {
                CreateMap<(User u, Preferences p), ProfileDto>();
            }
        }

        [Fact]
        public async Task Map_FromTuple_ShouldResolveFromMultipleSources()
        {
            // Arrange
            var user = new User("Alice", 30);
            var prefs = new Preferences("Dark", true);
            var source = (u: user, p: prefs);
            var mapper = new MapperConfiguration(cfg => cfg.AddProfile<MultiSourceProfile>()).CreateMapper();

            // Act
            var result = await mapper.MapAsync<ProfileDto>(source);

            // Assert
            Assert.Equal("Alice", result.Name);
            Assert.Equal(30, result.Age);
            Assert.Equal("Dark", result.Theme);
            Assert.Equal(true, result.NotificationsEnabled);
        }

        public record SourceA(string A);
        public record SourceB(string B);
        public record NestedDest(string A, string B);

        public class NestedTupleProfile : Profile
        {
            public NestedTupleProfile()
            {
                CreateMap<(SourceA a, SourceB b), NestedDest>();
            }
        }

        [Fact]
        public async Task Map_FromNestedTuple_ShouldWork()
        {
            // Arrange
            var source = (a: new SourceA("ValA"), b: new SourceB("ValB"));
            var mapper = new MapperConfiguration(cfg => cfg.AddProfile<NestedTupleProfile>()).CreateMapper();

            // Act
            var result = await mapper.MapAsync<NestedDest>(source);

            // Assert
            Assert.Equal("ValA", result.A);
            Assert.Equal("ValB", result.B);
        }
    }
}
