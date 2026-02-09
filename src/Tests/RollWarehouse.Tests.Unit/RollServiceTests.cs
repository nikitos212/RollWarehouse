using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Moq;
using RollWarehouse.Application.Abstractions.Ports;
using RollWarehouse.Application.Services;
using RollWarehouse.Domain.Entities;
using Xunit;

namespace RollWarehouse.Tests.Unit
{
    public class RollServiceTests
    {
        private static Roll CreateRoll(string idSuffix, double length, double weight, DateTime added, DateTime? removed = null)
        {
            return new Roll
            {
                Id = Guid.ParseExact(("00000000-0000-0000-0000-" + idSuffix.PadLeft(12, '0')), "d"),
                Length = length,
                Weight = weight,
                DateAdded = added,
                DateRemoved = removed
            };
        }

        [Fact]
        public async Task AddRollAsync_ShouldCreateRoll_AndCallRepo()
        {
            var repo = new Mock<IRollRepository>();
            repo.Setup(r => r.AddAsync(It.IsAny<Roll>())).ReturnsAsync((Roll r) => r);
            var service = new RollService(repo.Object);
            var res = await service.AddRollAsync(10.5, 100.0);
            Assert.Equal(10.5, res.Length);
            Assert.Equal(100.0, res.Weight);
            repo.Verify(r => r.AddAsync(It.IsAny<Roll>()), Times.Once);
        }

        [Theory]
        [InlineData(0, 10)]
        [InlineData(-1, 10)]
        public async Task AddRollAsync_InvalidLength_Throws(double length, double weight)
        {
            var repo = new Mock<IRollRepository>();
            var service = new RollService(repo.Object);
            await Assert.ThrowsAsync<ArgumentException>(() => service.AddRollAsync(length, weight));
        }

        [Theory]
        [InlineData(10, 0)]
        [InlineData(10, -5)]
        public async Task AddRollAsync_InvalidWeight_Throws(double length, double weight)
        {
            var repo = new Mock<IRollRepository>();
            var service = new RollService(repo.Object);
            await Assert.ThrowsAsync<ArgumentException>(() => service.AddRollAsync(length, weight));
        }

        [Fact]
        public async Task DeleteRollAsync_NotFound_ReturnsNull_AndDoesNotCallDelete()
        {
            var repo = new Mock<IRollRepository>();
            repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Roll?)null);
            var service = new RollService(repo.Object);
            var res = await service.DeleteRollAsync(Guid.NewGuid());
            Assert.Null(res);
            repo.Verify(r => r.DeleteAsync(It.IsAny<Guid>()), Times.Never);
        }

        [Fact]
        public async Task DeleteRollAsync_Found_CallsDeleteAndReturnsRoll()
        {
            var existing = CreateRoll("1", 5, 2, DateTime.UtcNow.AddDays(-2));
            var repo = new Mock<IRollRepository>();
            repo.Setup(r => r.GetByIdAsync(existing.Id)).ReturnsAsync(existing);
            repo.Setup(r => r.DeleteAsync(existing.Id)).ReturnsAsync(() =>
            {
                existing.DateRemoved = DateTime.UtcNow;
                return existing;
            });
            var service = new RollService(repo.Object);
            var res = await service.DeleteRollAsync(existing.Id);
            Assert.NotNull(res);
            Assert.Equal(existing.Id, res!.Id);
            repo.Verify(r => r.DeleteAsync(existing.Id), Times.Once);
        }

        [Fact]
        public async Task ListFilteredAsync_PassesFilterToRepository()
        {
            var filter = new RollFilter(Guid.NewGuid(), null, 1, 10, null, null, null, null, null, null);
            var repo = new Mock<IRollRepository>();
            repo.Setup(r => r.ListFilteredAsync(It.IsAny<RollFilter>()))
                .ReturnsAsync(new List<Roll> { CreateRoll("2", 3, 4, DateTime.UtcNow) });

            var service = new RollService(repo.Object);
            var result = await service.ListFilteredAsync(filter);
            Assert.NotNull(result);
            Assert.Single(result);
            repo.Verify(r => r.ListFilteredAsync(It.IsAny<RollFilter>()), Times.Once);
        }

        [Fact]
        public async Task GetStatisticsAsync_ComputesAggregatesCorrectly()
        {
            var start = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var end = new DateTime(2025, 1, 5, 0, 0, 0, DateTimeKind.Utc);

            var r1 = CreateRoll("3", 10, 5, start.AddHours(1), start.AddDays(1));
            var r2 = CreateRoll("4", 20, 10, start.AddDays(2), null);
            var r3 = CreateRoll("5", 15, 7, start.AddDays(-1), start.AddDays(1));

            var list = new List<Roll> { r1, r2, r3 };

            var repo = new Mock<IRollRepository>();
            repo.Setup(r => r.GetForPeriodAsync(start, end)).ReturnsAsync(list);

            var service = new RollService(repo.Object);
            var stat = await service.GetStatisticsAsync(start, end);

            Assert.Equal(2, stat.AddedCount);
            Assert.Equal(2, stat.RemovedCount);
            Assert.Equal(15.0, stat.AverageLength);
            Assert.Equal((5 + 10 + 7) / 3.0, stat.AverageWeight);
            Assert.Equal(20.0, stat.MaxLength);
            Assert.Equal(10.0, stat.MinLength);
            Assert.Equal(10.0, stat.MaxWeight);
            Assert.Equal(5.0, stat.MinWeight);
            Assert.Equal(22.0, stat.TotalWeight);
            Assert.NotNull(stat.MaxIntervalSeconds);
            Assert.NotNull(stat.MinIntervalSeconds);

            Assert.Equal(172800, stat.MaxIntervalSeconds);
            Assert.Equal(82800, stat.MinIntervalSeconds);
        }

        [Fact]
        public async Task GetStatisticsAsync_EmptyPeriod_ReturnsZeroCounts_AndNullAverages()
        {
            var start = new DateTime(2025, 2, 1, 0, 0, 0, DateTimeKind.Utc);
            var end = start.AddDays(1);
            var repo = new Mock<IRollRepository>();
            repo.Setup(r => r.GetForPeriodAsync(start, end)).ReturnsAsync(new List<Roll>());

            var service = new RollService(repo.Object);
            var stat = await service.GetStatisticsAsync(start, end);

            Assert.Equal(0, stat.AddedCount);
            Assert.Equal(0, stat.RemovedCount);
            Assert.Null(stat.AverageLength);
            Assert.Null(stat.AverageWeight);
            Assert.Null(stat.MaxLength);
            Assert.Null(stat.MinLength);
            Assert.Null(stat.MaxWeight);
            Assert.Null(stat.MinWeight);
            Assert.Equal(0, stat.TotalWeight);
            Assert.Null(stat.MaxIntervalSeconds);
            Assert.Null(stat.MinIntervalSeconds);
        }

        [Fact]
        public async Task GetPeriodDayExtremaAsync_ReturnsCorrectDaysForMinMax()
        {
            var start = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var end = new DateTime(2025, 1, 5, 0, 0, 0, DateTimeKind.Utc);

            var rollA = CreateRoll("6", 10, 5, start.AddHours(1), start.AddDays(1));
            var rollB = CreateRoll("7", 8, 3, start.AddDays(2), start.AddDays(4));
            var rollC = CreateRoll("8", 12, 20, start.AddDays(3).AddHours(1), start.AddDays(3).AddHours(2));

            var list = new List<Roll> { rollA, rollB, rollC };

            var repo = new Mock<IRollRepository>();
            repo.Setup(r => r.GetForPeriodAsync(start, end)).ReturnsAsync(list);

            var service = new RollService(repo.Object);
            var ext = await service.GetPeriodDayExtremaAsync(start, end);

            Assert.Equal(start.AddDays(1), ext.DayWithMinCount); // Jan2
            Assert.Equal(start.AddDays(3), ext.DayWithMaxCount); // Jan4
            Assert.Equal(start.AddDays(1), ext.DayWithMinTotalWeight);
            Assert.Equal(start.AddDays(3), ext.DayWithMaxTotalWeight);
        }
    }
}
