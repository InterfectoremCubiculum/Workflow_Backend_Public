using AutoMapper;
using Moq;
using System.Text;
using WorkflowTime.Exceptions;
using WorkflowTime.Features.Summary.Dtos;
using WorkflowTime.Features.Summary.Services;
using ZiggyCreatures.Caching.Fusion;

namespace WorkflowTime.Test
{
    public class SummaryCachedTest
    {
        [Fact]
        public async Task ExportCsv_ShouldReturnCsv_WhenDataIsFound()
        {
            // Arrange
            var token = Guid.NewGuid();
            var workSummaries = new List<WorkSummaryCSV>
            {
                new WorkSummaryCSV { Name = "Jan", Surname = "Kowalski", Email = "jan@example.com", ProjectName = "Projekt1", TeamName = "ZespółA", TotalWorkHours = TimeSpan.FromHours(40), TotalBreakHours = TimeSpan.FromHours(5), TotalDaysWorked = 5, TotalDaysOff = 0 }
            };
            var cacheMock = new Mock<IFusionCache>();
            cacheMock.Setup(c => c.GetOrDefaultAsync<List<WorkSummaryCSV>>(
                token.ToString(),
                default, 
                null,                          
                default
            )).ReturnsAsync(workSummaries); var mapperMock = new Mock<IMapper>();
            var summaryCached = new SummaryCached(cacheMock.Object, mapperMock.Object);

            // Act
            var result = await summaryCached.ExportCsv(token);

            // Assert
            var expectedCsv = "Name,Surname,Email,ProjectName,TeamName,TotalWorkHours,TotalBreakHours,TotalDaysWorked,TotalDaysOff\r\nJan,Kowalski,jan@example.com,Projekt1,ZespółA,01d 16:00:00,00d 05:00:00,5,0\r\n";
            var resultString = Encoding.UTF8.GetString(result);
            Assert.Equal(expectedCsv, resultString);
        }

        [Fact]
        public async Task ExportCsv_ShouldThrowNotFoundException_WhenDataIsNull()
        {
            var token = Guid.NewGuid();
            var cacheMock = new Mock<IFusionCache>();
            cacheMock.Setup(c => c.GetOrDefaultAsync<List<WorkSummaryCSV>>(
                token.ToString(),
                default,
                null,
                default
            )).ReturnsAsync((List<WorkSummaryCSV>)null);
            var mapperMock = new Mock<IMapper>(); 
            var summaryCached = new SummaryCached(cacheMock.Object, mapperMock.Object);

            await Assert.ThrowsAsync<NotFoundException>(() => summaryCached.ExportCsv(token));
        }

        [Fact]
        public async Task ExportCsv_ShouldThrowNotFoundException_WhenDataIsEmpty()
        {
            // Arrange
            var token = Guid.NewGuid();
            var cacheMock = new Mock<IFusionCache>();
            cacheMock.Setup(c => c.GetOrDefaultAsync<List<WorkSummaryCSV>>(
                token.ToString(),
                default,
                null,
                default
            )).ReturnsAsync(new List<WorkSummaryCSV>());
            var mapperMock = new Mock<IMapper>();
            var summaryCached = new SummaryCached(cacheMock.Object, mapperMock.Object);

            // Act & Assert
            await Assert.ThrowsAsync<NotFoundException>(() => summaryCached.ExportCsv(token));
        }
    }
}
