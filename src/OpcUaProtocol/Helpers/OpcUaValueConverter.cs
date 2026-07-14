// =============================================================================
// MAS.Communication
// https://www.mas-automation.com/
//
// Copyright 2026 MAS (厦门威光) Corporation
//
// Licensed under the Apache License, Version 2.0
// See LICENSE file in the project root for full license information.
// =============================================================================

using System.Globalization;
using Opc.Ua;

namespace MAS.Communication.OpcUaProtocol;

/// <summary>
/// 在库自有模型与官方 SDK 类型之间进行转换的内部帮助类
/// </summary>
internal static class OpcUaValueConverter {
    /// <summary>
    /// 将库自有节点标识解析为 SDK <see cref="NodeId"/>
    /// </summary>
    public static NodeId ToNodeId(OpcUaNodeId nodeId) {
        if (string.IsNullOrEmpty(nodeId.Value)) {
            throw new ArgumentException("节点标识不能为空。", nameof(nodeId));
        }

        return NodeId.Parse(nodeId.Value);
    }

    /// <summary>
    /// 将 SDK <see cref="DataValue"/> 转换为库自有读取结果
    /// </summary>
    public static OpcUaValue ToOpcUaValue(OpcUaNodeId nodeId, DataValue dataValue) {
        return new OpcUaValue(
            nodeId,
            dataValue.Value,
            dataValue.StatusCode.Code,
            ToNullableTimestamp(dataValue.SourceTimestamp),
            ToNullableTimestamp(dataValue.ServerTimestamp));
    }

    /// <summary>
    /// 将 SDK <see cref="NodeClass"/> 映射为库自有节点类别
    /// </summary>
    public static OpcUaNodeClass MapNodeClass(NodeClass nodeClass) {
        return nodeClass switch {
            NodeClass.Object => OpcUaNodeClass.Object,
            NodeClass.Variable => OpcUaNodeClass.Variable,
            NodeClass.Method => OpcUaNodeClass.Method,
            NodeClass.ObjectType => OpcUaNodeClass.ObjectType,
            NodeClass.VariableType => OpcUaNodeClass.VariableType,
            NodeClass.ReferenceType => OpcUaNodeClass.ReferenceType,
            NodeClass.DataType => OpcUaNodeClass.DataType,
            NodeClass.View => OpcUaNodeClass.View,
            _ => OpcUaNodeClass.Unspecified
        };
    }

    /// <summary>
    /// 将读取到的原始值转换为指定的目标类型
    /// </summary>
    public static T? ConvertValue<T>(object? value) {
        if (value is null) {
            return default;
        }

        if (value is T typed) {
            return typed;
        }

        Type target = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);

        if (target.IsEnum) {
            return (T)Enum.ToObject(target, value);
        }

        if (value is IConvertible) {
            return (T)Convert.ChangeType(value, target, CultureInfo.InvariantCulture);
        }

        return (T)value;
    }

    /// <summary>
    /// 判断 OPC UA 状态码是否表示“错误（Bad）”
    /// </summary>
    public static bool IsBad(uint statusCode) {
        return (statusCode & 0x80000000u) != 0u;
    }

    private static DateTime? ToNullableTimestamp(DateTime timestamp) {
        return timestamp == DateTime.MinValue ? null : timestamp;
    }
}
