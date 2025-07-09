# RevitAdjustWall

A Revit API add-in for adjusting wall gaps based on specified distances and connection rules.

## Overview

RevitAdjustWall is a comprehensive Revit add-in that allows users to automatically adjust wall gaps according to predefined business rules. The application follows MVVM (Model-View-ViewModel) pattern and implements SOLID design principles for maintainable and extensible code.

## Features

- **Interactive Wall Selection**: Pick walls manually or select all walls in the active view
- **Configurable Gap Distance**: Specify gap distance in millimeters with real-time validation
- **Multiple Connection Types**: Supports Inline, Corner, T-Shape, and Tri-Shape wall connections
- **Input Validation**: Comprehensive validation for numeric inputs with user-friendly error messages
- **Modern WPF Interface**: Clean, responsive user interface with proper data binding

## System Requirements

- **Revit**: 2025 or later
- **.NET Framework**: 8.0
- **Operating System**: Windows 10/11

## Installation

1. Build the project in Release mode
2. Copy the following files to your Revit add-ins folder:
   - `RevitAdjustWall.dll`
   - `RevitAdjustWall.addin`
3. Restart Revit
4. The command will appear in the Add-ins tab

## Usage

1. Open a Revit project with walls
2. Navigate to Add-ins → External Tools → Adjust Wall
3. In the dialog:
   - Enter the desired gap distance in millimeters
   - Click "Pick Wall" to manually select walls, or "Active View" to select all walls in the current view
   - Click "Adjust Walls" to apply the changes

## Architecture

### Project Structure

```
RevitAdjustWall/
├── Commands/
│   └── AdjustWallCommand.cs          # Main IExternalCommand implementation
├── Models/
│   ├── WallAdjustmentModel.cs        # Data model for wall operations
│   └── WallConnectionType.cs         # Connection type definitions
├── Services/
│   ├── IWallSelectionService.cs      # Wall selection interface
│   ├── WallSelectionService.cs       # Wall selection implementation
│   ├── IWallAdjustmentService.cs     # Wall adjustment interface
│   └── WallAdjustmentService.cs      # Wall adjustment implementation
├── ViewModels/
│   ├── BaseViewModel.cs              # Base ViewModel with INotifyPropertyChanged
│   ├── RelayCommand.cs               # Command implementation for MVVM
│   └── WallAdjustmentViewModel.cs    # Main ViewModel
├── Views/
│   ├── WallAdjustmentView.xaml       # WPF user interface
│   └── WallAdjustmentView.xaml.cs    # Code-behind with input validation
├── Converters/
│   ├── InverseBooleanToVisibilityConverter.cs
│   └── StringToVisibilityConverter.cs
├── Validation/
│   └── InputValidator.cs             # Input validation logic
├── Exceptions/
│   └── WallAdjustmentException.cs    # Custom exceptions
└── RevitAdjustWall.addin             # Add-in manifest
```

### Design Patterns

- **MVVM Pattern**: Separation of UI, business logic, and data
- **Command Pattern**: IExternalCommand implementation
- **Service Pattern**: Abstracted business logic services
- **Repository Pattern**: Data access abstraction
- **Dependency Injection**: Constructor injection for services

### SOLID Principles

- **Single Responsibility**: Each class has one reason to change
- **Open/Closed**: Open for extension, closed for modification
- **Liskov Substitution**: Derived classes are substitutable for base classes
- **Interface Segregation**: Clients depend only on interfaces they use
- **Dependency Inversion**: Depend on abstractions, not concretions

## Business Logic

The application handles four types of wall connections:

1. **Inline**: Two walls connected end-to-end in a straight line
2. **Corner**: Two walls meeting at a corner (90-degree angle)
3. **T-Shape**: Two walls where one connects perpendicularly to the middle of another
4. **Tri-Shape**: Three walls connected at a single point

## Input Validation

- Gap distance must be between 0.1mm and 10,000mm
- Only numeric input is allowed in the gap distance field
- Real-time validation with descriptive error messages
- Automatic input sanitization

## Error Handling

- Comprehensive exception handling throughout the application
- Custom exception types for specific error scenarios
- User-friendly error messages
- Graceful degradation on operation failures

## Development

### Building the Project

1. Clone the repository
2. Open in Visual Studio 2022 or later
3. Restore NuGet packages
4. Build in Release mode

### Testing

The project includes unit tests for core business logic:

```bash
# Run tests using Visual Studio Test Explorer
# or use command line:
dotnet test
```

### Contributing

1. Follow the existing code style and patterns
2. Add unit tests for new functionality
3. Update documentation as needed
4. Ensure all tests pass before submitting

## License

This project is licensed under the MIT License - see the LICENSE file for details.

## Support

For issues and questions, please create an issue in the project repository.
