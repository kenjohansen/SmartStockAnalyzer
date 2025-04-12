# Code Documentation Standards

## File Headers
```csharp
// Copyright (c) 2025 Ken Johansen. All rights reserved.
// This file is part of Smart Portfolio Analyzer and is licensed under the MIT license.
// See the LICENSE file in the project root for more information.
```

## Class Documentation
```csharp
/// <summary>
/// Provides [brief description of class purpose]
/// </summary>
/// <remarks>
/// [Detailed explanation of class functionality]
/// </remarks>
public class [ClassName]
{
    // ... class implementation
}
```

## Method Documentation
```csharp
/// <summary>
/// [Brief description of method purpose]
/// </summary>
/// <param name="[paramName]">[Description of parameter]</param>
/// <returns>[Description of return value]</returns>
/// <exception cref="[ExceptionType]">[When this exception is thrown]</exception>
/// <remarks>
/// [Additional notes or important information]
/// </remarks>
public [ReturnType] [MethodName]([ParameterType] [paramName])
{
    // ... method implementation
}
```

## Property Documentation
```csharp
/// <summary>
/// Gets or sets [description of property]
/// </summary>
/// <value>
/// [Description of property value]
/// </value>
public [Type] [PropertyName] { get; set; }
```

## Documentation Guidelines

1. All public and protected members must be documented
2. Use complete sentences in documentation
3. Include examples where appropriate
4. Document exception conditions
5. Use proper XML documentation tags:
   - <summary> - Brief description
   - <remarks> - Additional details
   - <param> - Parameter descriptions
   - <returns> - Return value description
   - <exception> - Exception conditions
   - <value> - Property value description
   - <example> - Code examples

## Naming Conventions

1. Use PascalCase for class names
2. Use camelCase for method and property names
3. Use PascalCase for enum members
4. Use UPPER_SNAKE_CASE for constants
5. Use _camelCase for private fields

## Code Style

1. Use 4-space indentation
2. Maximum line length: 120 characters
3. Use var for local variables when type is obvious
4. Use async/await for asynchronous operations
5. Use expression-bodied members where appropriate
