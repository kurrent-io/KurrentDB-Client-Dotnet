using KurrentDB.Client.Core.Serialization;

namespace KurrentDB.Client.Tests.Core.Serialization;

public class MessageTypeNamingResolutionContextTests
{
	[Fact]
	public void CategoryName_ExtractsFromStreamName()
	{
		// Arrange
		var context = MessageTypeNamingResolutionContext.FromStreamName("user-123");
            
		// Act
		var categoryName = context.CategoryName;
            
		// Assert
		Assert.Equal("user", categoryName);
	}
	
	[Fact]
	public void CategoryName_ExtractsFromStreamNameWithMoreThanOneDash()
	{
		// Arrange
		var context = MessageTypeNamingResolutionContext.FromStreamName("user-some-123");
            
		// Act
		var categoryName = context.CategoryName;
            
		// Assert
		Assert.Equal("user", categoryName);
	}
	
	[Fact]
	public void CategoryName_ReturnsTheWholeStreamName()
	{
		// Arrange
		var context = MessageTypeNamingResolutionContext.FromStreamName("user123");
            
		// Act
		var categoryName = context.CategoryName;
            
		// Assert
		Assert.Equal("user123", categoryName);
	}
}
