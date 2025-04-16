using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using System.Linq.Expressions;
using Xcaciv.LooseNotes.Web.Data;

namespace Xcaciv.LooseNotes.Web.Tests.Fixtures
{
    public class TestBase : IDisposable
    {
        protected readonly ApplicationDbContext _context;
        protected readonly Mock<ILogger<object>> _loggerMock;
        
        public TestBase()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            
            _context = new ApplicationDbContext(options);
            _loggerMock = new Mock<ILogger<object>>();
            
            SeedDatabase();
        }

        protected virtual void SeedDatabase()
        {
            // Base implementation does nothing, override in derived classes
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        // Helper method to create mock DbSet for testing
        protected Mock<DbSet<T>> MockDbSet<T>(IQueryable<T> data) where T : class
        {
            var mockSet = new Mock<DbSet<T>>();
            mockSet.As<IQueryable<T>>().Setup(m => m.Provider).Returns(data.Provider);
            mockSet.As<IQueryable<T>>().Setup(m => m.Expression).Returns(data.Expression);
            mockSet.As<IQueryable<T>>().Setup(m => m.ElementType).Returns(data.ElementType);
            mockSet.As<IQueryable<T>>().Setup(m => m.GetEnumerator()).Returns(data.GetEnumerator);
            
            return mockSet;
        }
    }
}