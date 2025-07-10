using NUnit.Framework;
using RevitAdjustWall.Models;
using Moq;
using System.Collections.Generic;
using Autodesk.Revit.DB;

namespace RevitAdjustWall.Tests.Models;

[TestFixture]
public class WallAdjustmentModelTests
{
    private WallAdjustmentModel _model;
    private Mock<Wall> _mockWall1;
    private Mock<Wall> _mockWall2;

    [SetUp]
    public void SetUp()
    {
        _model = new WallAdjustmentModel();
        _mockWall1 = new Mock<Wall>();
        _mockWall2 = new Mock<Wall>();
    }

    [Test]
    public void GapDistanceMm_DefaultValue_ShouldBe0()
    {
        // Arrange & Act
        var defaultGapDistance = _model.GapDistanceMm;

        // Assert
        Assert.That(defaultGapDistance, Is.EqualTo(0.0));
    }

    [Test]
    public void GapDistanceMm_SetValidValue_ShouldUpdateProperty()
    {
        // Arrange
        var newGapDistance = 25.5;

        // Act
        _model.GapDistanceMm = newGapDistance;

        // Assert
        Assert.That(_model.GapDistanceMm, Is.EqualTo(newGapDistance));
    }

    [Test]
    public void SelectedWalls_InitialState_ShouldBeEmpty()
    {
        // Arrange & Act
        var selectedWalls = _model.SelectedWalls;

        // Assert
        Assert.That(selectedWalls, Is.Not.Null);
        Assert.That(selectedWalls.Count, Is.EqualTo(0));
    }

    [Test]
    public void SelectedWalls_AddWalls_ShouldContainAddedWalls()
    {
        // Arrange
        var walls = new List<Wall> { _mockWall1.Object, _mockWall2.Object };

        // Act
        foreach (var wall in walls)
        {
            _model.SelectedWalls.Add(wall);
        }

        // Assert
        Assert.That(_model.SelectedWalls.Count, Is.EqualTo(2));
        Assert.That(_model.SelectedWalls, Contains.Item(_mockWall1.Object));
        Assert.That(_model.SelectedWalls, Contains.Item(_mockWall2.Object));
    }

    [Test]
    public void IsValid_WithValidData_ShouldReturnTrue()
    {
        // Arrange
        _model.GapDistanceMm = 10.0;
        _model.SelectedWalls.Add(_mockWall1.Object);
        _model.Document = new Mock<Document>().Object;

        // Act
        var isValid = _model.IsValid();

        // Assert
        Assert.That(isValid, Is.True);
    }

    [Test]
    public void IsValid_WithInvalidData_ShouldReturnFalse()
    {
        // Arrange - model with default values (invalid)

        // Act
        var isValid = _model.IsValid();

        // Assert
        Assert.That(isValid, Is.False);
    }
}