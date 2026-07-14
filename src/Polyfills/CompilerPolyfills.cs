// =============================================================================
// MAS.Communication
// https://www.mas-automation.com/
//
// Copyright 2026 MAS (厦门威光) Corporation
//
// Licensed under the Apache License, Version 2.0
// See LICENSE file in the project root for full license information.
// =============================================================================

#if NETFRAMEWORK

namespace System.Runtime.CompilerServices;

/// <summary>
/// 为 .NET Framework 目标提供 <c>init</c> 访问器与 <c>record</c> 类型所需的编译器占位类型
/// </summary>
/// <remarks>该类型在现代 .NET 运行时中已由 BCL 提供，仅在缺失时补齐</remarks>
internal static class IsExternalInit { }

#endif
