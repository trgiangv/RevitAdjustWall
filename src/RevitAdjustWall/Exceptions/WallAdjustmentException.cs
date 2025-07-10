using System;

namespace RevitAdjustWall.Exceptions;

/// <summary>
/// Custom exception for wall adjustment operations
/// Provides specific error handling for the application domain
/// </summary>
public class WallAdjustmentException : Exception
{
    /// <summary>
    /// Gets the error code associated with this exception
    /// </summary>
    public string ErrorCode { get; }

    /// <summary>
    /// Initializes a new instance of WallAdjustmentException
    /// </summary>
    public WallAdjustmentException() : base()
    {
    }

    /// <summary>
    /// Initializes a new instance of WallAdjustmentException with a message
    /// </summary>
    /// <param name="message">The error message</param>
    public WallAdjustmentException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of WallAdjustmentException with a message and error code
    /// </summary>
    /// <param name="message">The error message</param>
    /// <param name="errorCode">The error code</param>
    public WallAdjustmentException(string message, string errorCode) : base(message)
    {
        ErrorCode = errorCode;
    }

    /// <summary>
    /// Initializes a new instance of WallAdjustmentException with a message and inner exception
    /// </summary>
    /// <param name="message">The error message</param>
    /// <param name="innerException">The inner exception</param>
    public WallAdjustmentException(string message, Exception innerException) : base(message, innerException)
    {
    }

    /// <summary>
    /// Initializes a new instance of WallAdjustmentException with a message, error code, and inner exception
    /// </summary>
    /// <param name="message">The error message</param>
    /// <param name="errorCode">The error code</param>
    /// <param name="innerException">The inner exception</param>
    public WallAdjustmentException(string message, string errorCode, Exception innerException) : base(message, innerException)
    {
        ErrorCode = errorCode;
    }
}

/// <summary>
/// Exception thrown when wall selection is invalid
/// </summary>
public class InvalidWallSelectionException : WallAdjustmentException
{
    public InvalidWallSelectionException(string message) : base(message, "INVALID_WALL_SELECTION")
    {
    }

    public InvalidWallSelectionException(string message, Exception innerException) : base(message, "INVALID_WALL_SELECTION", innerException)
    {
    }
}

/// <summary>
/// Exception thrown when gap distance is invalid
/// </summary>
public class InvalidGapDistanceException : WallAdjustmentException
{
    public InvalidGapDistanceException(string message) : base(message, "INVALID_GAP_DISTANCE")
    {
    }

    public InvalidGapDistanceException(string message, Exception innerException) : base(message, "INVALID_GAP_DISTANCE", innerException)
    {
    }
}

/// <summary>
/// Exception thrown when wall adjustment operation fails
/// </summary>
public class WallAdjustmentOperationException : WallAdjustmentException
{
    public WallAdjustmentOperationException(string message) : base(message, "ADJUSTMENT_OPERATION_FAILED")
    {
    }

    public WallAdjustmentOperationException(string message, Exception innerException) : base(message, "ADJUSTMENT_OPERATION_FAILED", innerException)
    {
    }
}